using TravelPlannerApp.Application.Abstractions.CurrentUser;
using TravelPlannerApp.Application.Abstractions.Persistence;
using TravelPlannerApp.Application.Abstractions.Realtime;
using TravelPlannerApp.Application.Common.Exceptions;
using TravelPlannerApp.Application.Common.Utilities;
using TravelPlannerApp.Application.Contracts.Itineraries;
using TravelPlannerApp.Application.Mappings;
using TravelPlannerApp.Domain.Entities;

namespace TravelPlannerApp.Application.Services;

public sealed class ItineraryService : IItineraryService
{
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly IUserRepository _userRepository;
    private readonly IItineraryRepository _itineraryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IItineraryRealtimeNotifier _notifier;

    public ItineraryService(
        ICurrentUserAccessor currentUserAccessor,
        IUserRepository userRepository,
        IItineraryRepository itineraryRepository,
        IUnitOfWork unitOfWork,
        IItineraryRealtimeNotifier notifier)
    {
        _currentUserAccessor = currentUserAccessor;
        _userRepository = userRepository;
        _itineraryRepository = itineraryRepository;
        _unitOfWork = unitOfWork;
        _notifier = notifier;
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
        var membersToRemove = itinerary.Members
            .Where(member => !requestedUserIds.Contains(member.UserId, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (membersToRemove.Count > 0)
        {
            _itineraryRepository.RemoveMembers(membersToRemove);
        }

        var membersToAdd = new List<ItineraryMember>();
        foreach (var userId in requestedUserIds)
        {
            if (currentMemberLookup.ContainsKey(userId))
            {
                currentMemberLookup[userId].User ??= userLookup[userId];
                continue;
            }

            var member = new ItineraryMember
            {
                ItineraryId = itinerary.Id,
                UserId = userId,
                AddedByUserId = currentUser.Id,
                AddedAtUtc = DateTime.UtcNow,
                User = userLookup[userId],
                AddedByUser = currentUser
            };

            itinerary.Members.Add(member);
            membersToAdd.Add(member);
        }

        if (membersToAdd.Count > 0)
        {
            await _itineraryRepository.AddMembersAsync(membersToAdd, cancellationToken);
        }

        itinerary.ConcurrencyToken = ConcurrencyTokenHelper.NewToken();
        itinerary.UpdatedAtUtc = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var response = await ListMemberResponsesAsync(itinerary.Id, cancellationToken);

        await _notifier.NotifyAsync(
            itinerary.Id,
            new ItineraryRealtimeNotification("itinerary.members.updated", itinerary.Id, itinerary.Id, itinerary.UpdatedAtUtc, response),
            cancellationToken);

        return response;
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

        _itineraryRepository.RemoveMembers([memberToRemove]);
        itinerary.ConcurrencyToken = ConcurrencyTokenHelper.NewToken();
        itinerary.UpdatedAtUtc = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var response = await ListMemberResponsesAsync(itinerary.Id, cancellationToken);
        await _notifier.NotifyAsync(
            itinerary.Id,
            new ItineraryRealtimeNotification("itinerary.members.updated", itinerary.Id, itinerary.Id, itinerary.UpdatedAtUtc, response),
            cancellationToken);

        return response;
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
}
