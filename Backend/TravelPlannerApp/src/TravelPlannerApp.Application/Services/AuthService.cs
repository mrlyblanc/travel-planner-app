using TravelPlannerApp.Application.Abstractions.CurrentUser;
using TravelPlannerApp.Application.Abstractions.Persistence;
using TravelPlannerApp.Application.Abstractions.Security;
using TravelPlannerApp.Application.Common.Exceptions;
using TravelPlannerApp.Application.Common.Utilities;
using TravelPlannerApp.Application.Contracts.Auth;
using TravelPlannerApp.Application.Contracts.Users;
using TravelPlannerApp.Application.Mappings;
using TravelPlannerApp.Domain.Entities;

namespace TravelPlannerApp.Application.Services;

public sealed class AuthService : IAuthService
{
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly JwtOptions _jwtOptions;
    private readonly TimeProvider _timeProvider;

    public AuthService(
        ICurrentUserAccessor currentUserAccessor,
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IRefreshTokenGenerator refreshTokenGenerator,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordResetTokenRepository passwordResetTokenRepository,
        IUnitOfWork unitOfWork,
        JwtOptions jwtOptions,
        TimeProvider timeProvider)
    {
        _currentUserAccessor = currentUserAccessor;
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _refreshTokenGenerator = refreshTokenGenerator;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordResetTokenRepository = passwordResetTokenRepository;
        _unitOfWork = unitOfWork;
        _jwtOptions = jwtOptions;
        _timeProvider = timeProvider;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (user is null || !_passwordHasher.VerifyHashedPassword(user.PasswordHash, request.Password))
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var token = request.RefreshToken.Trim();
        var tokenHash = _refreshTokenGenerator.HashToken(token);
        var refreshToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (refreshToken?.User is null
            || refreshToken.RevokedAtUtc.HasValue
            || refreshToken.ExpiresAtUtc <= now)
        {
            throw new UnauthorizedException("Invalid or expired refresh token.");
        }

        refreshToken.RevokedAtUtc = now;

        var replacementToken = _refreshTokenGenerator.GenerateToken();
        refreshToken.ReplacedByTokenHash = _refreshTokenGenerator.HashToken(replacementToken);

        var replacementEntity = CreateRefreshTokenEntity(refreshToken.UserId, replacementToken, now);
        await _refreshTokenRepository.AddAsync(replacementEntity, cancellationToken);

        EnsureAuthVersion(refreshToken.User);
        var accessToken = _jwtTokenGenerator.GenerateToken(refreshToken.User);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            accessToken.AccessToken,
            replacementToken,
            "Bearer",
            accessToken.ExpiresAtUtc,
            replacementEntity.ExpiresAtUtc,
            refreshToken.User.ToResponse());
    }

    public async Task LogoutAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var token = request.RefreshToken.Trim();
        var tokenHash = _refreshTokenGenerator.HashToken(token);
        var refreshToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);
        if (refreshToken is null || refreshToken.RevokedAtUtc.HasValue)
        {
            return;
        }

        refreshToken.RevokedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<ForgotPasswordResponse> RequestPasswordResetAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (user is null)
        {
            return new ForgotPasswordResponse(
                "If that email is registered, a password reset link is ready.",
                null);
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var existingTokens = await _passwordResetTokenRepository.ListByUserIdAsync(user.Id, cancellationToken);
        foreach (var existingToken in existingTokens.Where(static token => !token.UsedAtUtc.HasValue && !token.RevokedAtUtc.HasValue))
        {
            existingToken.RevokedAtUtc = now;
        }

        var rawToken = _refreshTokenGenerator.GenerateToken();
        await _passwordResetTokenRepository.AddAsync(new PasswordResetToken
        {
            Id = IdGenerator.New("prt"),
            UserId = user.Id,
            TokenHash = _refreshTokenGenerator.HashToken(rawToken),
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddMinutes(30)
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ForgotPasswordResponse(
            "If that email is registered, a password reset link is ready.",
            rawToken);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var tokenHash = _refreshTokenGenerator.HashToken(request.Token.Trim());
        var passwordResetToken = await _passwordResetTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (passwordResetToken?.User is null
            || passwordResetToken.UsedAtUtc.HasValue
            || passwordResetToken.RevokedAtUtc.HasValue
            || passwordResetToken.ExpiresAtUtc <= now)
        {
            throw new BadRequestException("Reset link is invalid or has expired.");
        }

        if (_passwordHasher.VerifyHashedPassword(passwordResetToken.User.PasswordHash, request.NewPassword))
        {
            throw new BadRequestException("New password must be different from the current password.");
        }

        passwordResetToken.User.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        passwordResetToken.User.AuthVersion = GenerateAuthVersion();
        passwordResetToken.UsedAtUtc = now;

        var refreshTokens = await _refreshTokenRepository.ListByUserIdAsync(passwordResetToken.UserId, cancellationToken);
        foreach (var refreshToken in refreshTokens.Where(static token => !token.RevokedAtUtc.HasValue))
        {
            refreshToken.RevokedAtUtc = now;
        }

        var passwordResetTokens = await _passwordResetTokenRepository.ListByUserIdAsync(passwordResetToken.UserId, cancellationToken);
        foreach (var otherToken in passwordResetTokens.Where(token => token.Id != passwordResetToken.Id && !token.UsedAtUtc.HasValue && !token.RevokedAtUtc.HasValue))
        {
            otherToken.RevokedAtUtc = now;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentUserEntityAsync(cancellationToken);
        if (!_passwordHasher.VerifyHashedPassword(user.PasswordHash, request.CurrentPassword))
        {
            throw new UnauthorizedException("Current password is incorrect.");
        }

        if (string.Equals(request.CurrentPassword, request.NewPassword, StringComparison.Ordinal))
        {
            throw new BadRequestException("New password must be different from the current password.");
        }

        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        user.AuthVersion = GenerateAuthVersion();

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var refreshTokens = await _refreshTokenRepository.ListByUserIdAsync(user.Id, cancellationToken);
        foreach (var refreshToken in refreshTokens.Where(static token => !token.RevokedAtUtc.HasValue))
        {
            refreshToken.RevokedAtUtc = now;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<UserResponse> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentUserEntityAsync(cancellationToken);
        return user.ToResponse();
    }

    private async Task<AuthResponse> IssueTokensAsync(User user, CancellationToken cancellationToken)
    {
        EnsureAuthVersion(user);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var refreshToken = _refreshTokenGenerator.GenerateToken();
        var refreshTokenEntity = CreateRefreshTokenEntity(user.Id, refreshToken, now);
        var accessToken = _jwtTokenGenerator.GenerateToken(user);

        await _refreshTokenRepository.AddAsync(refreshTokenEntity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            accessToken.AccessToken,
            refreshToken,
            "Bearer",
            accessToken.ExpiresAtUtc,
            refreshTokenEntity.ExpiresAtUtc,
            user.ToResponse());
    }

    private RefreshToken CreateRefreshTokenEntity(string userId, string refreshToken, DateTime createdAtUtc)
    {
        return new RefreshToken
        {
            Id = IdGenerator.New("rtk"),
            UserId = userId,
            TokenHash = _refreshTokenGenerator.HashToken(refreshToken),
            ExpiresAtUtc = createdAtUtc.AddDays(_jwtOptions.RefreshTokenLifetimeDays),
            CreatedAtUtc = createdAtUtc
        };
    }

    private async Task<User> GetCurrentUserEntityAsync(CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserAccessor.GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            throw new UnauthorizedException("Authenticated user is required.");
        }

        return await _userRepository.GetByIdAsync(currentUserId.Trim(), cancellationToken)
            ?? throw new UnauthorizedException($"Current user '{currentUserId}' was not found.");
    }

    private static void EnsureAuthVersion(User user)
    {
        if (string.IsNullOrWhiteSpace(user.AuthVersion))
        {
            user.AuthVersion = GenerateAuthVersion();
        }
    }

    private static string GenerateAuthVersion()
    {
        return Guid.NewGuid().ToString("N");
    }
}
