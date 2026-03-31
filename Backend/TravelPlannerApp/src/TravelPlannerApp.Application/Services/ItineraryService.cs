using TravelPlannerApp.Application.Abstractions.CurrentUser;
using TravelPlannerApp.Application.Abstractions.Persistence;
using TravelPlannerApp.Application.Abstractions.Realtime;
using TravelPlannerApp.Application.Common.Exceptions;
using TravelPlannerApp.Application.Common.Utilities;
using TravelPlannerApp.Application.Contracts.Itineraries;
using TravelPlannerApp.Application.Contracts.Notifications;
using TravelPlannerApp.Application.Mappings;
using TravelPlannerApp.Domain.Entities;

namespace TravelPlannerApp.Application.Services;

public sealed class ItineraryService : IItineraryService
{
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly IUserRepository _userRepository;
    private readonly IItineraryRepository _itineraryRepository;
    private readonly IUserNotificationRepository _userNotificationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IItineraryRealtimeNotifier _notifier;
    private readonly IUserRealtimeNotifier _userRealtimeNotifier;

    public ItineraryService(
        ICurrentUserAccessor currentUserAccessor,
        IUserRepository userRepository,
        IItineraryRepository itineraryRepository,
        IUserNotificationRepository userNotificationRepository,
        IUnitOfWork unitOfWork,
        IItineraryRealtimeNotifier notifier,
        IUserRealtimeNotifier userRealtimeNotifier)
    {
        _currentUserAccessor = currentUserAccessor;
        _userRepository = userRepository;
        _itineraryRepository = itineraryRepository;
        _userNotificationRepository = userNotificationRepository;
        _unitOfWork = unitOfWork;
        _notifier = notifier;
        _userRealtimeNotifier = userRealtimeNotifier;
    }

    public async Task<IReadOnlyList<ItineraryResponse>> GetAccessibleItinerariesAsync(CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        var itineraries = await _itineraryRepository.ListAccessibleAsync(currentUser.Id, cancellationToken);

        return itineraries
            .OrderBy(static itinerary => itinerary.StartDate)
            .ThenBy(static itinerary => itinerary.Title, StringComparer.OrdinalIgnoreCase)
            .Select(static itinerary => itinerary.ToResponse())
            .ToList();
    }

    public async Task<ItineraryResponse> GetItineraryByIdAsync(string itineraryId, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        var itinerary = await _itineraryRepository.GetAccessibleByIdAsync(currentUser.Id, itineraryId, cancellationToken);
        if (itinerary is null)
        {
            await ThrowItineraryAccessExceptionAsync(itineraryId, cancellationToken);
        }

        return itinerary!.ToResponse();
    }

    public async Task<ItineraryResponse> CreateItineraryAsync(CreateItineraryRequest request, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        var now = DateTime.UtcNow;

        var itinerary = new Itinerary
        {
            Id = IdGenerator.New("itinerary"),
            ConcurrencyToken = ConcurrencyTokenHelper.NewToken(),
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            Destination = request.Destination.Trim(),
            ShareCode = await GenerateUniqueShareCodeAsync(cancellationToken),
            ShareCodeUpdatedAtUtc = now,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            CreatedById = currentUser.Id,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        itinerary.Members.Add(new ItineraryMember
        {
            ItineraryId = itinerary.Id,
            UserId = currentUser.Id,
            AddedByUserId = currentUser.Id,
            AddedAtUtc = now,
            User = currentUser,
            AddedByUser = currentUser
        });

        await _itineraryRepository.AddAsync(itinerary, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _notifier.NotifyAsync(
            itinerary.Id,
            new ItineraryRealtimeNotification("itinerary.created", itinerary.Id, itinerary.Id, now, itinerary.ToResponse()),
            cancellationToken);

        return itinerary.ToResponse();
    }

    public async Task<ItineraryResponse> UpdateItineraryAsync(string itineraryId, string? expectedVersion, UpdateItineraryRequest request, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        var itinerary = await _itineraryRepository.GetAccessibleByIdAsync(currentUser.Id, itineraryId, cancellationToken);
        if (itinerary is null)
        {
            await ThrowItineraryAccessExceptionAsync(itineraryId, cancellationToken);
        }

        ConcurrencyTokenHelper.EnsureMatches(itinerary!.ConcurrencyToken, expectedVersion);
        itinerary.Title = request.Title.Trim();
        itinerary.Description = request.Description?.Trim();
        itinerary.Destination = request.Destination.Trim();
        itinerary.StartDate = request.StartDate;
        itinerary.EndDate = request.EndDate;
        itinerary.ConcurrencyToken = ConcurrencyTokenHelper.NewToken();
        itinerary.UpdatedAtUtc = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _notifier.NotifyAsync(
            itinerary.Id,
            new ItineraryRealtimeNotification("itinerary.updated", itinerary.Id, itinerary.Id, itinerary.UpdatedAtUtc, itinerary.ToResponse()),
            cancellationToken);

        return itinerary.ToResponse();
    }

    public async Task<ItineraryShareCodeResponse> GetShareCodeAsync(string itineraryId, CancellationToken cancellationToken = default)
    {
        var itinerary = await GetOwnedItineraryAsync(itineraryId, cancellationToken);

        return new ItineraryShareCodeResponse(
            itinerary.Id,
            itinerary.ConcurrencyToken,
            itinerary.ShareCode,
            itinerary.ShareCodeUpdatedAtUtc);
    }

    public async Task<ItineraryShareCodeResponse> RotateShareCodeAsync(string itineraryId, string? expectedVersion, CancellationToken cancellationToken = default)
    {
        var itinerary = await GetOwnedItineraryAsync(itineraryId, cancellationToken);
        ConcurrencyTokenHelper.EnsureMatches(itinerary.ConcurrencyToken, expectedVersion);

        itinerary.ShareCode = await GenerateUniqueShareCodeAsync(cancellationToken, itinerary.Id);
        itinerary.ShareCodeUpdatedAtUtc = DateTime.UtcNow;
        itinerary.ConcurrencyToken = ConcurrencyTokenHelper.NewToken();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ItineraryShareCodeResponse(
            itinerary.Id,
            itinerary.ConcurrencyToken,
            itinerary.ShareCode,
            itinerary.ShareCodeUpdatedAtUtc);
    }

    public async Task<ItineraryResponse> JoinByCodeAsync(JoinItineraryByCodeRequest request, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        var shareCode = request.Code.Trim();
        var itinerary = await _itineraryRepository.GetByShareCodeAsync(shareCode, cancellationToken);
        if (itinerary is null)
        {
            throw new BadRequestException("That itinerary code is invalid.");
        }

        if (itinerary.Members.Any(member => string.Equals(member.UserId, currentUser.Id, StringComparison.OrdinalIgnoreCase)))
        {
            throw new BadRequestException("You are already part of this itinerary.");
        }

        var now = DateTime.UtcNow;
        var newMember = new ItineraryMember
        {
            ItineraryId = itinerary.Id,
            UserId = currentUser.Id,
            AddedByUserId = itinerary.CreatedById,
            AddedAtUtc = now,
            User = currentUser
        };

        itinerary.Members.Add(newMember);
        await _itineraryRepository.AddMembersAsync([newMember], cancellationToken);

        itinerary.ConcurrencyToken = ConcurrencyTokenHelper.NewToken();
        itinerary.UpdatedAtUtc = now;

        var notifications = new List<UserNotification>();
        if (!string.Equals(itinerary.CreatedById, currentUser.Id, StringComparison.OrdinalIgnoreCase))
        {
            notifications.Add(CreateNotification(
                itinerary.CreatedById,
                "itinerary.member.joined",
                "New collaborator joined",
                $"{currentUser.Name} joined {itinerary.Title}.",
                itinerary.Id,
                currentUser.Id,
                now));
        }

        notifications.Add(CreateNotification(
            currentUser.Id,
            "itinerary.member.added",
            "You joined an itinerary",
            $"You joined {itinerary.Title} with a share code.",
            itinerary.Id,
            itinerary.CreatedById,
            now));

        await _userNotificationRepository.AddRangeAsync(notifications, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await NotifyMembershipChangedAsync(itinerary, cancellationToken);
        await NotifyUsersAsync(notifications, cancellationToken);

        return itinerary.ToResponse();
    }

    public async Task<IReadOnlyList<ItineraryMemberResponse>> GetMembersAsync(string itineraryId, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        var isMember = await _itineraryRepository.IsMemberAsync(itineraryId, currentUser.Id, cancellationToken);
        if (!isMember)
        {
            await ThrowItineraryAccessExceptionAsync(itineraryId, cancellationToken);
        }

        return await ListMemberResponsesAsync(itineraryId, cancellationToken);
    }

    public async Task<IReadOnlyList<ItineraryMemberResponse>> ReplaceMembersAsync(string itineraryId, string? expectedVersion, ReplaceItineraryMembersRequest request, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        var itinerary = await _itineraryRepository.GetAccessibleByIdAsync(currentUser.Id, itineraryId, cancellationToken);
        if (itinerary is null)
        {
            await ThrowItineraryAccessExceptionAsync(itineraryId, cancellationToken);
        }

        ConcurrencyTokenHelper.EnsureMatches(itinerary!.ConcurrencyToken, expectedVersion);

        var requestedUserIds = request.UserIds
            .Where(static userId => !string.IsNullOrWhiteSpace(userId))
            .Select(static userId => userId.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!requestedUserIds.Contains(itinerary.CreatedById, StringComparer.OrdinalIgnoreCase))
        {
            requestedUserIds.Add(itinerary.CreatedById);
        }

        var users = await _userRepository.ListByIdsAsync(requestedUserIds, cancellationToken);
        var userLookup = users.ToDictionary(static user => user.Id, StringComparer.OrdinalIgnoreCase);
        var missingUserIds = requestedUserIds
            .Where(requestedUserId => !userLookup.ContainsKey(requestedUserId))
            .ToList();

        if (missingUserIds.Count > 0)
        {
            throw new BadRequestException($"Unknown members: {string.Join(", ", missingUserIds)}.");
        }

        var currentMemberLookup = itinerary.Members.ToDictionary(static member => member.UserId, StringComparer.OrdinalIgnoreCase);
        var newMemberUserIds = requestedUserIds
            .Where(requestedUserId => !currentMemberLookup.ContainsKey(requestedUserId))
            .ToList();

        if (newMemberUserIds.Count > 0)
        {
            throw new BadRequestException("New contributors must join with the itinerary share code.");
        }

        var membersToRemove = itinerary.Members
            .Where(member => !requestedUserIds.Contains(member.UserId, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (membersToRemove.Count > 0)
        {
            _itineraryRepository.RemoveMembers(membersToRemove);
            itinerary.ConcurrencyToken = ConcurrencyTokenHelper.NewToken();
            itinerary.UpdatedAtUtc = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await NotifyMembershipChangedAsync(itinerary, cancellationToken);
        }

        return await ListMemberResponsesAsync(itinerary.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<ItineraryMemberResponse>> RemoveMemberAsync(string itineraryId, string userId, string? expectedVersion, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        var itinerary = await _itineraryRepository.GetAccessibleByIdAsync(currentUser.Id, itineraryId, cancellationToken);
        if (itinerary is null)
        {
            await ThrowItineraryAccessExceptionAsync(itineraryId, cancellationToken);
        }

        ConcurrencyTokenHelper.EnsureMatches(itinerary!.ConcurrencyToken, expectedVersion);

        var normalizedUserId = userId.Trim();
        if (string.Equals(itinerary.CreatedById, normalizedUserId, StringComparison.OrdinalIgnoreCase))
        {
            throw new BadRequestException("The itinerary owner cannot be removed.");
        }

        var memberToRemove = itinerary.Members.FirstOrDefault(member =>
            string.Equals(member.UserId, normalizedUserId, StringComparison.OrdinalIgnoreCase));

        if (memberToRemove is null)
        {
            throw new NotFoundException($"Member '{normalizedUserId}' was not found in itinerary '{itineraryId}'.");
        }

        var now = DateTime.UtcNow;
        var removedUser = memberToRemove.User
            ?? await _userRepository.GetByIdAsync(normalizedUserId, cancellationToken)
            ?? throw new NotFoundException($"User '{normalizedUserId}' was not found.");

        _itineraryRepository.RemoveMembers([memberToRemove]);
        itinerary.ConcurrencyToken = ConcurrencyTokenHelper.NewToken();
        itinerary.UpdatedAtUtc = now;

        var notification = CreateNotification(
            removedUser.Id,
            "itinerary.member.removed",
            "Removed from itinerary",
            $"{currentUser.Name} removed you from {itinerary.Title}.",
            itinerary.Id,
            currentUser.Id,
            now);

        await _userNotificationRepository.AddRangeAsync([notification], cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await NotifyMembershipChangedAsync(itinerary, cancellationToken);
        await NotifyUsersAsync([notification], cancellationToken);

        return await ListMemberResponsesAsync(itinerary.Id, cancellationToken);
    }

    private async Task<User> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserAccessor.GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            throw new UnauthorizedException("Authenticated user is required.");
        }

        var currentUser = await _userRepository.GetByIdAsync(currentUserId.Trim(), cancellationToken);
        if (currentUser is null)
        {
            throw new UnauthorizedException($"Current user '{currentUserId}' was not found.");
        }

        return currentUser;
    }

    private async Task<Itinerary> GetOwnedItineraryAsync(string itineraryId, CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        var itinerary = await _itineraryRepository.GetAccessibleByIdAsync(currentUser.Id, itineraryId, cancellationToken);
        if (itinerary is null)
        {
            await ThrowItineraryAccessExceptionAsync(itineraryId, cancellationToken);
        }

        if (!string.Equals(itinerary!.CreatedById, currentUser.Id, StringComparison.OrdinalIgnoreCase))
        {
            throw new ForbiddenException($"You do not have access to itinerary '{itineraryId}'.");
        }

        return itinerary;
    }

    private async Task ThrowItineraryAccessExceptionAsync(string itineraryId, CancellationToken cancellationToken)
    {
        var itinerary = await _itineraryRepository.GetByIdAsync(itineraryId, cancellationToken);
        if (itinerary is null)
        {
            throw new NotFoundException($"Itinerary '{itineraryId}' was not found.");
        }

        throw new ForbiddenException($"You do not have access to itinerary '{itineraryId}'.");
    }

    private async Task<List<ItineraryMemberResponse>> ListMemberResponsesAsync(string itineraryId, CancellationToken cancellationToken)
    {
        var members = await _itineraryRepository.ListMembersAsync(itineraryId, cancellationToken);
        return members
            .OrderBy(static member => member.User?.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static member => member.UserId, StringComparer.OrdinalIgnoreCase)
            .Select(static member => member.ToResponse())
            .ToList();
    }

    private async Task NotifyMembershipChangedAsync(Itinerary itinerary, CancellationToken cancellationToken)
    {
        var response = await ListMemberResponsesAsync(itinerary.Id, cancellationToken);
        await _notifier.NotifyAsync(
            itinerary.Id,
            new ItineraryRealtimeNotification("itinerary.members.updated", itinerary.Id, itinerary.Id, itinerary.UpdatedAtUtc, response),
            cancellationToken);
    }

    private async Task NotifyUsersAsync(IEnumerable<UserNotification> notifications, CancellationToken cancellationToken)
    {
        foreach (var notification in notifications)
        {
            await _userRealtimeNotifier.NotifyUsersAsync([notification.UserId], notification.ToResponse(), cancellationToken);
        }
    }

    private static UserNotification CreateNotification(
        string userId,
        string type,
        string title,
        string message,
        string? itineraryId,
        string? actorUserId,
        DateTime createdAtUtc)
    {
        return new UserNotification
        {
            Id = IdGenerator.New("notification"),
            UserId = userId,
            Type = type,
            Title = title,
            Message = message,
            ItineraryId = itineraryId,
            ActorUserId = actorUserId,
            CreatedAtUtc = createdAtUtc
        };
    }

    private async Task<string> GenerateUniqueShareCodeAsync(CancellationToken cancellationToken, string? excludeItineraryId = null)
    {
        for (var attempt = 0; attempt < 25; attempt++)
        {
            var shareCode = ShareCodeGenerator.NewFiveDigitCode();
            var exists = await _itineraryRepository.ShareCodeExistsAsync(shareCode, excludeItineraryId, cancellationToken);
            if (!exists)
            {
                return shareCode;
            }
        }

        throw new InvalidOperationException("Could not generate a unique itinerary share code.");
    }
}
