using TravelPlannerApp.Application.Common.Exceptions;
using TravelPlannerApp.Application.Abstractions.Security;
using TravelPlannerApp.Application.Contracts.Auth;
using TravelPlannerApp.Application.Services;
using TravelPlannerApp.Application.Tests.Support;

namespace TravelPlannerApp.Application.Tests.Services;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsTokenAndUser()
    {
        var userRepository = new FakeUserRepository();
        userRepository.Users.Add(TestDataFactory.CreateUser("user-ava", "Ava Santos", "ava@example.com"));
        var refreshTokenRepository = new FakeRefreshTokenRepository();

        var service = new AuthService(
            new FakeCurrentUserAccessor(),
            userRepository,
            new FakePasswordHasher(),
            new FakeJwtTokenGenerator(),
            new FakeRefreshTokenGenerator(),
            refreshTokenRepository,
            new FakePasswordResetTokenRepository(),
            new FakeUnitOfWork(),
            new JwtOptions { Issuer = "tests", Audience = "tests", Secret = "12345678901234567890123456789012", RefreshTokenLifetimeDays = 14 },
            TimeProvider.System);

        var response = await service.LoginAsync(new LoginRequest
        {
            Email = "ava@example.com",
            Password = "Pass12345!"
        });

        Assert.Equal("token-for-user-ava-user-ava-auth-v1", response.AccessToken);
        Assert.Equal("refresh-token-01-abcdefghijklmnopqrstuvwxyz0123456789", response.RefreshToken);
        Assert.Equal("user-ava", response.User.Id);
        Assert.Single(refreshTokenRepository.RefreshTokens);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ThrowsUnauthorizedException()
    {
        var userRepository = new FakeUserRepository();
        userRepository.Users.Add(TestDataFactory.CreateUser("user-ava", "Ava Santos", "ava@example.com"));

        var service = new AuthService(
            new FakeCurrentUserAccessor(),
            userRepository,
            new FakePasswordHasher(),
            new FakeJwtTokenGenerator(),
            new FakeRefreshTokenGenerator(),
            new FakeRefreshTokenRepository(),
            new FakePasswordResetTokenRepository(),
            new FakeUnitOfWork(),
            new JwtOptions { Issuer = "tests", Audience = "tests", Secret = "12345678901234567890123456789012", RefreshTokenLifetimeDays = 14 },
            TimeProvider.System);

        await Assert.ThrowsAsync<UnauthorizedException>(() => service.LoginAsync(new LoginRequest
        {
            Email = "ava@example.com",
            Password = "wrong-password"
        }));
    }

    [Fact]
    public async Task GetCurrentUserAsync_WithoutAuthenticatedUser_ThrowsUnauthorizedException()
    {
        var service = new AuthService(
            new FakeCurrentUserAccessor(),
            new FakeUserRepository(),
            new FakePasswordHasher(),
            new FakeJwtTokenGenerator(),
            new FakeRefreshTokenGenerator(),
            new FakeRefreshTokenRepository(),
            new FakePasswordResetTokenRepository(),
            new FakeUnitOfWork(),
            new JwtOptions { Issuer = "tests", Audience = "tests", Secret = "12345678901234567890123456789012", RefreshTokenLifetimeDays = 14 },
            TimeProvider.System);

        await Assert.ThrowsAsync<UnauthorizedException>(() => service.GetCurrentUserAsync());
    }

    [Fact]
    public async Task RefreshAsync_WithActiveRefreshToken_RotatesRefreshToken()
    {
        var userRepository = new FakeUserRepository();
        var user = TestDataFactory.CreateUser("user-ava", "Ava Santos", "ava@example.com");
        userRepository.Users.Add(user);
        var refreshTokenRepository = new FakeRefreshTokenRepository();
        refreshTokenRepository.RefreshTokens.Add(new TravelPlannerApp.Domain.Entities.RefreshToken
        {
            Id = "rtk-1",
            UserId = user.Id,
            TokenHash = "hash::existing-refresh-token-abcdefghijklmnopqrstuvwxyz0123456789",
            CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ExpiresAtUtc = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            User = user
        });

        var service = new AuthService(
            new FakeCurrentUserAccessor(),
            userRepository,
            new FakePasswordHasher(),
            new FakeJwtTokenGenerator(),
            new FakeRefreshTokenGenerator(),
            refreshTokenRepository,
            new FakePasswordResetTokenRepository(),
            new FakeUnitOfWork(),
            new JwtOptions { Issuer = "tests", Audience = "tests", Secret = "12345678901234567890123456789012", RefreshTokenLifetimeDays = 14 },
            new FakeTimeProvider(new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero)));

        var response = await service.RefreshAsync(new RefreshTokenRequest
        {
            RefreshToken = "existing-refresh-token-abcdefghijklmnopqrstuvwxyz0123456789"
        });

        Assert.Equal("token-for-user-ava-user-ava-auth-v1", response.AccessToken);
        Assert.Equal("refresh-token-01-abcdefghijklmnopqrstuvwxyz0123456789", response.RefreshToken);
        Assert.NotNull(refreshTokenRepository.RefreshTokens[0].RevokedAtUtc);
        Assert.Equal(2, refreshTokenRepository.RefreshTokens.Count);
    }

    [Fact]
    public async Task RefreshAsync_WithRevokedRefreshToken_ThrowsUnauthorizedException()
    {
        var userRepository = new FakeUserRepository();
        var user = TestDataFactory.CreateUser("user-ava", "Ava Santos", "ava@example.com");
        userRepository.Users.Add(user);
        var refreshTokenRepository = new FakeRefreshTokenRepository();
        refreshTokenRepository.RefreshTokens.Add(new TravelPlannerApp.Domain.Entities.RefreshToken
        {
            Id = "rtk-1",
            UserId = user.Id,
            TokenHash = "hash::existing-refresh-token-abcdefghijklmnopqrstuvwxyz0123456789",
            CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ExpiresAtUtc = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            RevokedAtUtc = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            User = user
        });

        var service = new AuthService(
            new FakeCurrentUserAccessor(),
            userRepository,
            new FakePasswordHasher(),
            new FakeJwtTokenGenerator(),
            new FakeRefreshTokenGenerator(),
            refreshTokenRepository,
            new FakePasswordResetTokenRepository(),
            new FakeUnitOfWork(),
            new JwtOptions { Issuer = "tests", Audience = "tests", Secret = "12345678901234567890123456789012", RefreshTokenLifetimeDays = 14 },
            new FakeTimeProvider(new DateTimeOffset(2026, 1, 3, 0, 0, 0, TimeSpan.Zero)));

        await Assert.ThrowsAsync<UnauthorizedException>(() => service.RefreshAsync(new RefreshTokenRequest
        {
            RefreshToken = "existing-refresh-token-abcdefghijklmnopqrstuvwxyz0123456789"
        }));
    }

    [Fact]
    public async Task LogoutAsync_WithActiveRefreshToken_RevokesToken()
    {
        var refreshTokenRepository = new FakeRefreshTokenRepository();
        refreshTokenRepository.RefreshTokens.Add(new TravelPlannerApp.Domain.Entities.RefreshToken
        {
            Id = "rtk-1",
            UserId = "user-ava",
            TokenHash = "hash::refresh-token-01-abcdefghijklmnopqrstuvwxyz0123456789",
            CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ExpiresAtUtc = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc)
        });

        var service = new AuthService(
            new FakeCurrentUserAccessor(),
            new FakeUserRepository(),
            new FakePasswordHasher(),
            new FakeJwtTokenGenerator(),
            new FakeRefreshTokenGenerator(),
            refreshTokenRepository,
            new FakePasswordResetTokenRepository(),
            new FakeUnitOfWork(),
            new JwtOptions { Issuer = "tests", Audience = "tests", Secret = "12345678901234567890123456789012", RefreshTokenLifetimeDays = 14 },
            new FakeTimeProvider(new DateTimeOffset(2026, 1, 3, 0, 0, 0, TimeSpan.Zero)));

        await service.LogoutAsync(new RefreshTokenRequest
        {
            RefreshToken = "refresh-token-01-abcdefghijklmnopqrstuvwxyz0123456789"
        });

        Assert.NotNull(refreshTokenRepository.RefreshTokens[0].RevokedAtUtc);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithValidCurrentPassword_UpdatesPasswordRotatesAuthVersionAndRevokesExistingRefreshTokens()
    {
        var userRepository = new FakeUserRepository();
        var user = TestDataFactory.CreateUser("user-ava", "Ava Santos", "ava@example.com");
        var originalAuthVersion = user.AuthVersion;
        userRepository.Users.Add(user);
        var refreshTokenRepository = new FakeRefreshTokenRepository();
        var passwordResetTokenRepository = new FakePasswordResetTokenRepository();
        refreshTokenRepository.RefreshTokens.Add(new TravelPlannerApp.Domain.Entities.RefreshToken
        {
            Id = "rtk-1",
            UserId = user.Id,
            TokenHash = "hash::existing-refresh-token-abcdefghijklmnopqrstuvwxyz0123456789",
            CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ExpiresAtUtc = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc)
        });

        var service = new AuthService(
            new FakeCurrentUserAccessor { CurrentUserId = user.Id },
            userRepository,
            new FakePasswordHasher(),
            new FakeJwtTokenGenerator(),
            new FakeRefreshTokenGenerator(),
            refreshTokenRepository,
            passwordResetTokenRepository,
            new FakeUnitOfWork(),
            new JwtOptions { Issuer = "tests", Audience = "tests", Secret = "12345678901234567890123456789012", RefreshTokenLifetimeDays = 14 },
            new FakeTimeProvider(new DateTimeOffset(2026, 1, 4, 0, 0, 0, TimeSpan.Zero)));

        await service.ChangePasswordAsync(new ChangePasswordRequest
        {
            CurrentPassword = "Pass12345!",
            NewPassword = "NewPass123!",
            ConfirmNewPassword = "NewPass123!"
        });

        Assert.Equal("hashed::NewPass123!", user.PasswordHash);
        Assert.NotEqual(originalAuthVersion, user.AuthVersion);
        Assert.NotNull(refreshTokenRepository.RefreshTokens[0].RevokedAtUtc);
        Assert.Single(refreshTokenRepository.RefreshTokens);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithWrongCurrentPassword_ThrowsUnauthorizedException()
    {
        var userRepository = new FakeUserRepository();
        var user = TestDataFactory.CreateUser("user-ava", "Ava Santos", "ava@example.com");
        userRepository.Users.Add(user);

        var service = new AuthService(
            new FakeCurrentUserAccessor { CurrentUserId = user.Id },
            userRepository,
            new FakePasswordHasher(),
            new FakeJwtTokenGenerator(),
            new FakeRefreshTokenGenerator(),
            new FakeRefreshTokenRepository(),
            new FakePasswordResetTokenRepository(),
            new FakeUnitOfWork(),
            new JwtOptions { Issuer = "tests", Audience = "tests", Secret = "12345678901234567890123456789012", RefreshTokenLifetimeDays = 14 },
            TimeProvider.System);

        await Assert.ThrowsAsync<UnauthorizedException>(() => service.ChangePasswordAsync(new ChangePasswordRequest
        {
            CurrentPassword = "WrongPass123!",
            NewPassword = "NewPass123!",
            ConfirmNewPassword = "NewPass123!"
        }));
    }

    [Fact]
    public async Task ChangePasswordAsync_WithSamePassword_ThrowsBadRequestException()
    {
        var userRepository = new FakeUserRepository();
        var user = TestDataFactory.CreateUser("user-ava", "Ava Santos", "ava@example.com");
        userRepository.Users.Add(user);

        var service = new AuthService(
            new FakeCurrentUserAccessor { CurrentUserId = user.Id },
            userRepository,
            new FakePasswordHasher(),
            new FakeJwtTokenGenerator(),
            new FakeRefreshTokenGenerator(),
            new FakeRefreshTokenRepository(),
            new FakePasswordResetTokenRepository(),
            new FakeUnitOfWork(),
            new JwtOptions { Issuer = "tests", Audience = "tests", Secret = "12345678901234567890123456789012", RefreshTokenLifetimeDays = 14 },
            TimeProvider.System);

        await Assert.ThrowsAsync<BadRequestException>(() => service.ChangePasswordAsync(new ChangePasswordRequest
        {
            CurrentPassword = "Pass12345!",
            NewPassword = "Pass12345!",
            ConfirmNewPassword = "Pass12345!"
        }));
    }

    [Fact]
    public async Task RequestPasswordResetAsync_WithKnownEmail_CreatesResetTokenAndRevokesPreviousOnes()
    {
        var userRepository = new FakeUserRepository();
        var user = TestDataFactory.CreateUser("user-ava", "Ava Santos", "ava@example.com");
        userRepository.Users.Add(user);
        var passwordResetTokenRepository = new FakePasswordResetTokenRepository();
        passwordResetTokenRepository.Tokens.Add(new TravelPlannerApp.Domain.Entities.PasswordResetToken
        {
            Id = "prt-old",
            UserId = user.Id,
            TokenHash = "hash::old-token",
            CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ExpiresAtUtc = new DateTime(2026, 1, 1, 0, 30, 0, DateTimeKind.Utc)
        });

        var service = new AuthService(
            new FakeCurrentUserAccessor(),
            userRepository,
            new FakePasswordHasher(),
            new FakeJwtTokenGenerator(),
            new FakeRefreshTokenGenerator(),
            new FakeRefreshTokenRepository(),
            passwordResetTokenRepository,
            new FakeUnitOfWork(),
            new JwtOptions { Issuer = "tests", Audience = "tests", Secret = "12345678901234567890123456789012", RefreshTokenLifetimeDays = 14 },
            new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, 1, 0, 0, TimeSpan.Zero)));

        var response = await service.RequestPasswordResetAsync(new ForgotPasswordRequest
        {
            Email = "ava@example.com"
        });

        Assert.False(string.IsNullOrWhiteSpace(response.DevResetToken));
        Assert.Equal(2, passwordResetTokenRepository.Tokens.Count);
        Assert.NotNull(passwordResetTokenRepository.Tokens[0].RevokedAtUtc);
        Assert.Equal($"hash::{response.DevResetToken}", passwordResetTokenRepository.Tokens[1].TokenHash);
    }

    [Fact]
    public async Task ResetPasswordAsync_WithValidToken_UpdatesPasswordAndConsumesResetToken()
    {
        var userRepository = new FakeUserRepository();
        var user = TestDataFactory.CreateUser("user-ava", "Ava Santos", "ava@example.com");
        var originalAuthVersion = user.AuthVersion;
        userRepository.Users.Add(user);
        var refreshTokenRepository = new FakeRefreshTokenRepository();
        refreshTokenRepository.RefreshTokens.Add(new TravelPlannerApp.Domain.Entities.RefreshToken
        {
            Id = "rtk-1",
            UserId = user.Id,
            TokenHash = "hash::existing-refresh-token-abcdefghijklmnopqrstuvwxyz0123456789",
            CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ExpiresAtUtc = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc)
        });
        var passwordResetTokenRepository = new FakePasswordResetTokenRepository();
        passwordResetTokenRepository.Tokens.Add(new TravelPlannerApp.Domain.Entities.PasswordResetToken
        {
            Id = "prt-1",
            UserId = user.Id,
            TokenHash = "hash::valid-reset-token",
            CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ExpiresAtUtc = new DateTime(2026, 1, 1, 0, 30, 0, DateTimeKind.Utc),
            User = user
        });

        var service = new AuthService(
            new FakeCurrentUserAccessor(),
            userRepository,
            new FakePasswordHasher(),
            new FakeJwtTokenGenerator(),
            new FakeRefreshTokenGenerator(),
            refreshTokenRepository,
            passwordResetTokenRepository,
            new FakeUnitOfWork(),
            new JwtOptions { Issuer = "tests", Audience = "tests", Secret = "12345678901234567890123456789012", RefreshTokenLifetimeDays = 14 },
            new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 10, 0, TimeSpan.Zero)));

        await service.ResetPasswordAsync(new ResetPasswordRequest
        {
            Token = "valid-reset-token",
            NewPassword = "ResetPass123!",
            ConfirmNewPassword = "ResetPass123!"
        });

        Assert.Equal("hashed::ResetPass123!", user.PasswordHash);
        Assert.NotEqual(originalAuthVersion, user.AuthVersion);
        Assert.NotNull(passwordResetTokenRepository.Tokens[0].UsedAtUtc);
        Assert.NotNull(refreshTokenRepository.RefreshTokens[0].RevokedAtUtc);
    }
}
