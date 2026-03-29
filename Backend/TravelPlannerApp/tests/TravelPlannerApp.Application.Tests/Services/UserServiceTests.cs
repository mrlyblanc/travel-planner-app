using TravelPlannerApp.Application.Common.Exceptions;
using TravelPlannerApp.Application.Common.Utilities;
using TravelPlannerApp.Application.Contracts.Users;
using TravelPlannerApp.Application.Services;
using TravelPlannerApp.Application.Tests.Support;

namespace TravelPlannerApp.Application.Tests.Services;

public sealed class UserServiceTests
{
    [Fact]
    public async Task GetUsersAsync_ReturnsUsersSortedByName()
    {
        var userRepository = new FakeUserRepository();
        userRepository.Users.AddRange(
        [
            TestDataFactory.CreateUser("user-zed", "Zed Cruz", "zed@example.com"),
            TestDataFactory.CreateUser("user-ava", "Ava Santos", "ava@example.com"),
            TestDataFactory.CreateUser("user-luca", "Luca Reyes", "luca@example.com")
        ]);

        var service = new UserService(userRepository, new FakeUnitOfWork());

        var response = await service.GetUsersAsync();

        Assert.Equal(["Ava Santos", "Luca Reyes", "Zed Cruz"], response.Select(user => user.Name).ToArray());
    }

    [Fact]
    public async Task GetUserByIdAsync_WhenUserDoesNotExist_ThrowsNotFoundException()
    {
        var service = new UserService(new FakeUserRepository(), new FakeUnitOfWork());

        var action = () => service.GetUserByIdAsync("user-missing");

        await Assert.ThrowsAsync<NotFoundException>(action);
    }

    [Fact]
    public async Task CreateUserAsync_WithoutAvatar_NormalizesEmailAndGeneratesAvatar()
    {
        var userRepository = new FakeUserRepository();
        var unitOfWork = new FakeUnitOfWork();
        var service = new UserService(userRepository, unitOfWork);

        var response = await service.CreateUserAsync(new CreateUserRequest
        {
            Name = "Ava Santos",
            Email = " Ava.Santos@Example.com "
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

        var service = new UserService(userRepository, new FakeUnitOfWork());

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

        var service = new UserService(userRepository, new FakeUnitOfWork());

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

        var service = new UserService(userRepository, new FakeUnitOfWork());

        await Assert.ThrowsAsync<PreconditionFailedException>(() => service.UpdateUserAsync("user-ava", ConcurrencyTokenHelper.ToETag("stale-version"), new UpdateUserRequest
        {
            Name = "Ava Santos",
            Email = "ava@example.com"
        }));
    }
}
