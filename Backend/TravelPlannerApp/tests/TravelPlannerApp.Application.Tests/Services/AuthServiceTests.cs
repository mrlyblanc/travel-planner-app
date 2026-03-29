using TravelPlannerApp.Application.Common.Exceptions;
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

        var service = new AuthService(
            new FakeCurrentUserAccessor(),
            userRepository,
            new FakePasswordHasher(),
            new FakeJwtTokenGenerator());

        var response = await service.LoginAsync(new LoginRequest
        {
            Email = "ava@example.com",
            Password = "Pass12345!"
        });

        Assert.Equal("token-for-user-ava", response.AccessToken);
        Assert.Equal("user-ava", response.User.Id);
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
            new FakeJwtTokenGenerator());

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
            new FakeJwtTokenGenerator());

        await Assert.ThrowsAsync<UnauthorizedException>(() => service.GetCurrentUserAsync());
    }
}
