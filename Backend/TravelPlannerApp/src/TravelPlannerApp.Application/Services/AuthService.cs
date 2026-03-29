using TravelPlannerApp.Application.Abstractions.CurrentUser;
using TravelPlannerApp.Application.Abstractions.Persistence;
using TravelPlannerApp.Application.Abstractions.Security;
using TravelPlannerApp.Application.Common.Exceptions;
using TravelPlannerApp.Application.Contracts.Auth;
using TravelPlannerApp.Application.Contracts.Users;
using TravelPlannerApp.Application.Mappings;

namespace TravelPlannerApp.Application.Services;

public sealed class AuthService : IAuthService
{
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public AuthService(
        ICurrentUserAccessor currentUserAccessor,
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _currentUserAccessor = currentUserAccessor;
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (user is null || !_passwordHasher.VerifyHashedPassword(user.PasswordHash, request.Password))
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        var token = _jwtTokenGenerator.GenerateToken(user);
        return new AuthResponse(token.AccessToken, "Bearer", token.ExpiresAtUtc, user.ToResponse());
    }

    public async Task<UserResponse> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserAccessor.GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            throw new UnauthorizedException("Authenticated user is required.");
        }

        var user = await _userRepository.GetByIdAsync(currentUserId.Trim(), cancellationToken)
            ?? throw new UnauthorizedException($"Current user '{currentUserId}' was not found.");

        return user.ToResponse();
    }
}
