using TravelPlannerApp.Application.Abstractions.CurrentUser;
using TravelPlannerApp.Application.Abstractions.Persistence;
using TravelPlannerApp.Application.Abstractions.Security;
using TravelPlannerApp.Application.Common.Exceptions;
using TravelPlannerApp.Application.Common.Utilities;
using TravelPlannerApp.Application.Contracts.Users;
using TravelPlannerApp.Application.Mappings;
using TravelPlannerApp.Domain.Entities;

namespace TravelPlannerApp.Application.Services;

public sealed class UserService : IUserService
{
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public UserService(
        ICurrentUserAccessor currentUserAccessor,
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork)
    {
        _currentUserAccessor = currentUserAccessor;
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<UserLookupResponse>> SearchUsersAsync(SearchUsersRequest request, CancellationToken cancellationToken = default)
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

        var query = request.Query.Trim();
        if (query.Length < 2)
        {
            throw new BadRequestException("Query must be at least 2 characters.");
        }

        var limit = Math.Clamp(request.Limit, 1, 25);
        var users = await _userRepository.SearchAsync(query, currentUser.Id, limit, cancellationToken);
        return users
            .OrderBy(static user => user.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static user => user.Id, StringComparer.OrdinalIgnoreCase)
            .Select(static user => user.ToLookupResponse())
            .ToList();
    }

    public async Task<UserResponse> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException($"User '{userId}' was not found.");

        return user.ToResponse();
    }

    public async Task<UserResponse> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(request.Email);
        var existingUser = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (existingUser is not null)
        {
            throw new ConflictException($"Email '{normalizedEmail}' is already in use.");
        }

        var user = new User
        {
            Id = IdGenerator.New("user"),
            ConcurrencyToken = ConcurrencyTokenHelper.NewToken(),
            Name = request.Name.Trim(),
            Email = normalizedEmail,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            Avatar = ResolveAvatar(request.Avatar, request.Name),
            CreatedAtUtc = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return user.ToResponse();
    }

    public async Task<UserResponse> UpdateUserAsync(string userId, string? expectedVersion, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException($"User '{userId}' was not found.");
        ConcurrencyTokenHelper.EnsureMatches(user.ConcurrencyToken, expectedVersion);

        var normalizedEmail = NormalizeEmail(request.Email);
        var existingUser = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (existingUser is not null && !string.Equals(existingUser.Id, user.Id, StringComparison.OrdinalIgnoreCase))
        {
            throw new ConflictException($"Email '{normalizedEmail}' is already in use.");
        }

        user.Name = request.Name.Trim();
        user.Email = normalizedEmail;
        user.Avatar = ResolveAvatar(request.Avatar, user.Name);
        user.ConcurrencyToken = ConcurrencyTokenHelper.NewToken();

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return user.ToResponse();
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    private static string ResolveAvatar(string? avatar, string name)
    {
        return string.IsNullOrWhiteSpace(avatar)
            ? AvatarGenerator.Generate(name)
            : avatar.Trim();
    }
}
