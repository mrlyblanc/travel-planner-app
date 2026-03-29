using TravelPlannerApp.Application.Common.Exceptions;
using TravelPlannerApp.Application.Common.Utilities;
using TravelPlannerApp.Application.Contracts.Users;
using TravelPlannerApp.Application.Services;
using TravelPlannerApp.Application.Tests.Support;

namespace TravelPlannerApp.Application.Tests.Services;

public sealed class UserServiceTests
{
    [Fact]
    public async Task SearchUsersAsync_ReturnsMatchesSortedAndExcludesCurrentUser()
    {
        var userRepository = new FakeUserRepository();
        userRepository.Users.AddRange(
        [
            TestDataFactory.CreateUser("user-zed", "Zed Cruz", "zed@example.com"),
            TestDataFactory.CreateUser("user-ava", "Ava Santos", "ava@example.com"),
            TestDataFactory.CreateUser("user-luca", "Luca Reyes", "luca@example.com"),
            TestDataFactory.CreateUser("user-luna", "Luna Reyes", "luna@example.com")
        ]);

        var service = new UserService(
            new FakeCurrentUserAccessor { CurrentUserId = "user-luca" },
            userRepository,
            new FakePasswordHasher(),
            new FakeUnitOfWork());

        var response = await service.SearchUsersAsync(new SearchUsersRequest
        {
            Query = "rey",
            Limit = 10
        });

        Assert.Equal(["Luna Reyes"], response.Select(user => user.Name).ToArray());
    }

    [Fact]
    public async Task GetUserByIdAsync_WhenUserDoesNotExist_ThrowsNotFoundException()
    {
        var service = new UserService(new FakeCurrentUserAccessor(), new FakeUserRepository(), new FakePasswordHasher(), new FakeUnitOfWork());

        var action = () => service.GetUserByIdAsync("user-missing");

        await Assert.ThrowsAsync<NotFoundException>(action);
    }

    [Fact]
    public async Task CreateUserAsync_WithoutAvatar_NormalizesEmailAndGeneratesAvatar()
    {
        var userRepository = new FakeUserRepository();
        var unitOfWork = new FakeUnitOfWork();
        var service = new UserService(new FakeCurrentUserAccessor(), userRepository, new FakePasswordHasher(), unitOfWork);

        var response = await service.CreateUserAsync(new CreateUserRequest
        {
            Name = "Ava Santos",
            Email = " Ava.Santos@Example.com ",
            Password = "Pass12345!"
        });

        Assert.Equal("ava.santos@example.com", response.Email);
        Assert.Equal("AS", response.Avatar);
        Assert.False(string.IsNullOrWhiteSpace(response.Version));
        Assert.Single(userRepository.Users);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task UpdateUserAsync_WhenAnotherUserOwnsEmail_ThrowsConflictException()
    {
        var userRepository = new FakeUserRepository();
        userRepository.Users.AddRange(
        [
            TestDataFactory.CreateUser("user-ava", "Ava Santos", "ava@example.com"),
            TestDataFactory.CreateUser("user-luca", "Luca Reyes", "luca@example.com")
        ]);

        var service = new UserService(new FakeCurrentUserAccessor(), userRepository, new FakePasswordHasher(), new FakeUnitOfWork());

        var action = () => service.UpdateUserAsync("user-luca", "user-luca-v1", new UpdateUserRequest
        {
            Name = "Luca Reyes",
            Email = "ava@example.com"
        });

        await Assert.ThrowsAsync<ConflictException>(action);
    }

    [Fact]
    public async Task UpdateUserAsync_WithoutExpectedVersion_ThrowsPreconditionRequiredException()
    {
        var userRepository = new FakeUserRepository();
        userRepository.Users.Add(TestDataFactory.CreateUser("user-ava", "Ava Santos", "ava@example.com"));

        var service = new UserService(new FakeCurrentUserAccessor(), userRepository, new FakePasswordHasher(), new FakeUnitOfWork());

        await Assert.ThrowsAsync<PreconditionRequiredException>(() => service.UpdateUserAsync("user-ava", null, new UpdateUserRequest
        {
            Name = "Ava Santos",
            Email = "ava@example.com"
        }));
    }

    [Fact]
    public async Task UpdateUserAsync_WithStaleExpectedVersion_ThrowsPreconditionFailedException()
    {
        var userRepository = new FakeUserRepository();
        userRepository.Users.Add(TestDataFactory.CreateUser("user-ava", "Ava Santos", "ava@example.com"));

        var service = new UserService(new FakeCurrentUserAccessor(), userRepository, new FakePasswordHasher(), new FakeUnitOfWork());

        await Assert.ThrowsAsync<PreconditionFailedException>(() => service.UpdateUserAsync("user-ava", ConcurrencyTokenHelper.ToETag("stale-version"), new UpdateUserRequest
        {
            Name = "Ava Santos",
            Email = "ava@example.com"
        }));
    }

    [Fact]
    public async Task SearchUsersAsync_WithoutCurrentUser_ThrowsUnauthorizedException()
    {
        var service = new UserService(new FakeCurrentUserAccessor(), new FakeUserRepository(), new FakePasswordHasher(), new FakeUnitOfWork());

        await Assert.ThrowsAsync<UnauthorizedException>(() => service.SearchUsersAsync(new SearchUsersRequest
        {
            Query = "ava"
        }));
    }

    [Fact]
    public async Task SearchUsersAsync_WithShortQuery_ThrowsBadRequestException()
    {
        var userRepository = new FakeUserRepository();
        userRepository.Users.Add(TestDataFactory.CreateUser("user-ava", "Ava Santos", "ava@example.com"));
        var service = new UserService(
            new FakeCurrentUserAccessor { CurrentUserId = "user-ava" },
            userRepository,
            new FakePasswordHasher(),
            new FakeUnitOfWork());

        await Assert.ThrowsAsync<BadRequestException>(() => service.SearchUsersAsync(new SearchUsersRequest
        {
            Query = "a"
        }));
    }
}
