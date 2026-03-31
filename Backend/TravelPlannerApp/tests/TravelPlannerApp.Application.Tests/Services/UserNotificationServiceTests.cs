using TravelPlannerApp.Application.Common.Exceptions;
using TravelPlannerApp.Application.Services;
using TravelPlannerApp.Application.Tests.Support;
using TravelPlannerApp.Domain.Entities;

namespace TravelPlannerApp.Application.Tests.Services;

public sealed class UserNotificationServiceTests
{
    [Fact]
    public async Task DeleteAsync_WhenNotificationBelongsToCurrentUser_RemovesItAndSaves()
    {
        var ava = TestDataFactory.CreateUser("user-ava", "Ava Santos", "ava@example.com");
        var currentUser = new FakeCurrentUserAccessor { CurrentUserId = ava.Id };
        var userRepository = new FakeUserRepository();
        userRepository.Users.Add(ava);
        var notificationRepository = new FakeUserNotificationRepository();
        notificationRepository.Notifications.Add(new UserNotification
        {
            Id = "notif-1",
            UserId = ava.Id,
            Type = "itinerary.member.added",
            Title = "You joined an itinerary",
            Message = "You joined Tokyo Sakura Sprint.",
            ItineraryId = "itinerary-tokyo",
            ActorUserId = "user-luca",
            CreatedAtUtc = new DateTime(2026, 3, 30, 2, 0, 0, DateTimeKind.Utc),
        });
        var unitOfWork = new FakeUnitOfWork();

        var service = new UserNotificationService(currentUser, userRepository, notificationRepository, unitOfWork);

        await service.DeleteAsync("notif-1");

        Assert.Empty(notificationRepository.Notifications);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task DeleteAsync_WhenNotificationBelongsToAnotherUser_ThrowsNotFound()
    {
        var ava = TestDataFactory.CreateUser("user-ava", "Ava Santos", "ava@example.com");
        var luca = TestDataFactory.CreateUser("user-luca", "Luca Reyes", "luca@example.com");
        var currentUser = new FakeCurrentUserAccessor { CurrentUserId = ava.Id };
        var userRepository = new FakeUserRepository();
        userRepository.Users.AddRange([ava, luca]);
        var notificationRepository = new FakeUserNotificationRepository();
        notificationRepository.Notifications.Add(new UserNotification
        {
            Id = "notif-2",
            UserId = luca.Id,
            Type = "itinerary.member.joined",
            Title = "New collaborator joined",
            Message = "Ava joined Tokyo Sakura Sprint.",
            ItineraryId = "itinerary-tokyo",
            ActorUserId = ava.Id,
            CreatedAtUtc = new DateTime(2026, 3, 30, 3, 0, 0, DateTimeKind.Utc),
        });

        var service = new UserNotificationService(currentUser, userRepository, notificationRepository, new FakeUnitOfWork());

        await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync("notif-2"));
        Assert.Single(notificationRepository.Notifications);
    }
}
