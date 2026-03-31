using TravelPlannerApp.Application.Common.Exceptions;
using TravelPlannerApp.Application.Contracts.Itineraries;
using TravelPlannerApp.Application.Services;
using TravelPlannerApp.Application.Tests.Support;

namespace TravelPlannerApp.Application.Tests.Services;

public sealed class ItineraryServiceTests
{
    [Fact]
    public async Task GetAccessibleItinerariesAsync_ReturnsCurrentUserItinerariesSortedByDateThenTitle()
    {
        var ava = TestDataFactory.CreateUser("user-ava", "Ava Santos", "ava@example.com");
        var currentUser = new FakeCurrentUserAccessor { CurrentUserId = ava.Id };
        var userRepository = new FakeUserRepository();
        userRepository.Users.Add(ava);

        var repo = new FakeItineraryRepository();
        var laterTrip = TestDataFactory.CreateItinerary("itinerary-b", ava.Id, "Beta", new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 5));
        laterTrip.Members.Add(TestDataFactory.CreateMember(laterTrip, ava));
        laterTrip.ShareCode = "12345";
        var earlierTrip = TestDataFactory.CreateItinerary("itinerary-a", ava.Id, "Alpha", new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 5));
        earlierTrip.Members.Add(TestDataFactory.CreateMember(earlierTrip, ava));
        earlierTrip.ShareCode = "54321";
        repo.Itineraries.AddRange([laterTrip, earlierTrip]);

        var service = CreateService(currentUser, userRepository, repo);

        var response = await service.GetAccessibleItinerariesAsync();

        Assert.Equal(["itinerary-a", "itinerary-b"], response.Select(itinerary => itinerary.Id).ToArray());
    }

    [Fact]
    public async Task GetAccessibleItinerariesAsync_WithoutCurrentUser_ThrowsUnauthorizedException()
    {
        var service = CreateService(
            new FakeCurrentUserAccessor(),
            new FakeUserRepository(),
            new FakeItineraryRepository());

        await Assert.ThrowsAsync<UnauthorizedException>(() => service.GetAccessibleItinerariesAsync());
    }

    [Fact]
    public async Task GetItineraryByIdAsync_WhenCurrentUserIsNotMember_ThrowsForbiddenException()
    {
        var ava = TestDataFactory.CreateUser("user-ava", "Ava Santos", "ava@example.com");
        var luca = TestDataFactory.CreateUser("user-luca", "Luca Reyes", "luca@example.com");
        var itinerary = TestDataFactory.CreateItinerary("itinerary-tokyo", ava.Id);
        itinerary.Members.Add(TestDataFactory.CreateMember(itinerary, ava));

        var userRepository = new FakeUserRepository();
        userRepository.Users.AddRange([ava, luca]);
        var itineraryRepository = new FakeItineraryRepository();
        itineraryRepository.Itineraries.Add(itinerary);

        var service = CreateService(
            new FakeCurrentUserAccessor { CurrentUserId = luca.Id },
            userRepository,
            itineraryRepository);

        await Assert.ThrowsAsync<ForbiddenException>(() => service.GetItineraryByIdAsync(itinerary.Id));
    }

    [Fact]
    public async Task CreateItineraryAsync_AddsCreatorAsMemberAndSendsRealtimeNotification()
    {
        var ava = TestDataFactory.CreateUser("user-ava", "Ava Santos", "ava@example.com");
        var userRepository = new FakeUserRepository();
        userRepository.Users.Add(ava);
        var itineraryRepository = new FakeItineraryRepository();
        var notifier = new FakeRealtimeNotifier();
        var unitOfWork = new FakeUnitOfWork();

        var service = CreateService(
            new FakeCurrentUserAccessor { CurrentUserId = ava.Id },
            userRepository,
            itineraryRepository,
            unitOfWork,
            notifier);

        var response = await service.CreateItineraryAsync(new CreateItineraryRequest
        {
            Title = "Tokyo Sprint",
            Destination = "Tokyo",
            StartDate = new DateOnly(2026, 4, 14),
            EndDate = new DateOnly(2026, 4, 18)
        });

        var itinerary = Assert.Single(itineraryRepository.Itineraries);
        Assert.Equal(ava.Id, itinerary.CreatedById);
        Assert.Contains(itinerary.Members, member => member.UserId == ava.Id);
        Assert.Equal(response.Id, itinerary.Id);
        Assert.False(string.IsNullOrWhiteSpace(response.Version));
        Assert.Matches("^[0-9]{5}$", itinerary.ShareCode);
        Assert.Single(notifier.Notifications);
        Assert.Equal("itinerary.created", notifier.Notifications[0].Type);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task UpdateItineraryAsync_WhenAccessible_UpdatesFieldsAndNotifies()
    {
        var ava = TestDataFactory.CreateUser("user-ava", "Ava Santos", "ava@example.com");
        var itinerary = TestDataFactory.CreateItinerary("itinerary-tokyo", ava.Id, "Old title");
        itinerary.Members.Add(TestDataFactory.CreateMember(itinerary, ava));

        var userRepository = new FakeUserRepository();
        userRepository.Users.Add(ava);
        var itineraryRepository = new FakeItineraryRepository();
        itineraryRepository.Itineraries.Add(itinerary);
        var notifier = new FakeRealtimeNotifier();

        var service = CreateService(
            new FakeCurrentUserAccessor { CurrentUserId = ava.Id },
            userRepository,
            itineraryRepository,
            notifier: notifier);

        var originalVersion = itinerary.ConcurrencyToken;
        var response = await service.UpdateItineraryAsync(itinerary.Id, originalVersion, new UpdateItineraryRequest
        {
            Title = "New title",
            Destination = "Seoul",
            Description = "Updated",
            StartDate = new DateOnly(2026, 4, 20),
            EndDate = new DateOnly(2026, 4, 25)
        });

        Assert.Equal("New title", itinerary.Title);
        Assert.Equal("Seoul", itinerary.Destination);
        Assert.Equal(response.Title, itinerary.Title);
        Assert.NotEqual(originalVersion, response.Version);
        Assert.Single(notifier.Notifications);
        Assert.Equal("itinerary.updated", notifier.Notifications[0].Type);
    }

    [Fact]
    public async Task GetShareCodeAsync_WhenCurrentUserOwnsItinerary_ReturnsCodeAndVersion()
    {
        var ava = TestDataFactory.CreateUser("user-ava", "Ava Santos", "ava@example.com");
        var itinerary = TestDataFactory.CreateItinerary("itinerary-tokyo", ava.Id);
        itinerary.Members.Add(TestDataFactory.CreateMember(itinerary, ava));
        itinerary.ShareCode = "48152";

        var userRepository = new FakeUserRepository();
        userRepository.Users.Add(ava);
        var itineraryRepository = new FakeItineraryRepository();
        itineraryRepository.Itineraries.Add(itinerary);

        var service = CreateService(
            new FakeCurrentUserAccessor { CurrentUserId = ava.Id },
            userRepository,
            itineraryRepository);

        var response = await service.GetShareCodeAsync(itinerary.Id);

        Assert.Equal("48152", response.Code);
        Assert.Equal(itinerary.ConcurrencyToken, response.Version);
    }

    [Fact]
    public async Task RotateShareCodeAsync_WhenCurrentUserOwnsItinerary_ChangesCodeAndVersion()
    {
        var ava = TestDataFactory.CreateUser("user-ava", "Ava Santos", "ava@example.com");
        var itinerary = TestDataFactory.CreateItinerary("itinerary-tokyo", ava.Id);
        itinerary.Members.Add(TestDataFactory.CreateMember(itinerary, ava));
        itinerary.ShareCode = "48152";

        var userRepository = new FakeUserRepository();
        userRepository.Users.Add(ava);
        var itineraryRepository = new FakeItineraryRepository();
        itineraryRepository.Itineraries.Add(itinerary);
        var unitOfWork = new FakeUnitOfWork();

        var service = CreateService(
            new FakeCurrentUserAccessor { CurrentUserId = ava.Id },
            userRepository,
            itineraryRepository,
            unitOfWork);

        var originalVersion = itinerary.ConcurrencyToken;
        var originalShareCode = itinerary.ShareCode;

        var response = await service.RotateShareCodeAsync(itinerary.Id, originalVersion);

        Assert.NotEqual(originalShareCode, response.Code);
        Assert.NotEqual(originalVersion, response.Version);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task JoinByCodeAsync_WhenCodeIsValid_AddsMemberAndSendsNotifications()
    {
        var ava = TestDataFactory.CreateUser("user-ava", "Ava Santos", "ava@example.com");
        var noah = TestDataFactory.CreateUser("user-noah", "Noah Tan", "noah@example.com");
        var itinerary = TestDataFactory.CreateItinerary("itinerary-tokyo", ava.Id);
        itinerary.ShareCode = "48152";
        itinerary.Members.Add(TestDataFactory.CreateMember(itinerary, ava));

        var userRepository = new FakeUserRepository();
        userRepository.Users.AddRange([ava, noah]);
        var itineraryRepository = new FakeItineraryRepository();
        itineraryRepository.Itineraries.Add(itinerary);
        var notificationRepository = new FakeUserNotificationRepository();
        var realtimeNotifier = new FakeRealtimeNotifier();
        var userRealtimeNotifier = new FakeUserRealtimeNotifier();

        var service = CreateService(
            new FakeCurrentUserAccessor { CurrentUserId = noah.Id },
            userRepository,
            itineraryRepository,
            notificationRepository,
            new FakeUnitOfWork(),
            realtimeNotifier,
            userRealtimeNotifier);

        var response = await service.JoinByCodeAsync(new JoinItineraryByCodeRequest { Code = "48152" });

        Assert.Equal(itinerary.Id, response.Id);
        Assert.Contains(itinerary.Members, member => member.UserId == noah.Id);
        Assert.Equal(2, notificationRepository.Notifications.Count);
        Assert.Contains(notificationRepository.Notifications, notification => notification.UserId == ava.Id && notification.Type == "itinerary.member.joined");
        Assert.Contains(notificationRepository.Notifications, notification => notification.UserId == noah.Id && notification.Type == "itinerary.member.added");
        Assert.Equal(2, userRealtimeNotifier.Notifications.Count);
        Assert.Single(realtimeNotifier.Notifications);
        Assert.Equal("itinerary.members.updated", realtimeNotifier.Notifications[0].Type);
    }

    [Fact]
    public async Task ReplaceMembersAsync_WhenCreatorOmitted_KeepsCreatorAsMemberAndNotifies()
    {
        var ava = TestDataFactory.CreateUser("user-ava", "Ava Santos", "ava@example.com");
        var luca = TestDataFactory.CreateUser("user-luca", "Luca Reyes", "luca@example.com");
        var mina = TestDataFactory.CreateUser("user-mina", "Mina Park", "mina@example.com");

        var itinerary = TestDataFactory.CreateItinerary("itinerary-tokyo", ava.Id);
        itinerary.Members.Add(TestDataFactory.CreateMember(itinerary, ava));
        itinerary.Members.Add(TestDataFactory.CreateMember(itinerary, luca));
        itinerary.Members.Add(TestDataFactory.CreateMember(itinerary, mina));

        var userRepository = new FakeUserRepository();
        userRepository.Users.AddRange([ava, luca, mina]);
        var itineraryRepository = new FakeItineraryRepository();
        itineraryRepository.Itineraries.Add(itinerary);
        var notifier = new FakeRealtimeNotifier();

        var service = CreateService(
            new FakeCurrentUserAccessor { CurrentUserId = ava.Id },
            userRepository,
            itineraryRepository,
            notifier: notifier);

        var originalVersion = itinerary.ConcurrencyToken;
        var response = await service.ReplaceMembersAsync(itinerary.Id, originalVersion, new ReplaceItineraryMembersRequest
        {
            UserIds = [luca.Id]
        });

        Assert.Contains(response, member => member.UserId == ava.Id);
        Assert.DoesNotContain(response, member => member.UserId == mina.Id);
        Assert.Single(notifier.Notifications);
        Assert.Equal("itinerary.members.updated", notifier.Notifications[0].Type);
        Assert.NotEqual(originalVersion, itinerary.ConcurrencyToken);
    }

    [Fact]
    public async Task ReplaceMembersAsync_WhenAddingNewContributor_ThrowsBadRequestException()
    {
        var ava = TestDataFactory.CreateUser("user-ava", "Ava Santos", "ava@example.com");
        var mina = TestDataFactory.CreateUser("user-mina", "Mina Park", "mina@example.com");
        var itinerary = TestDataFactory.CreateItinerary("itinerary-tokyo", ava.Id);
        itinerary.Members.Add(TestDataFactory.CreateMember(itinerary, ava));

        var userRepository = new FakeUserRepository();
        userRepository.Users.AddRange([ava, mina]);
        var itineraryRepository = new FakeItineraryRepository();
        itineraryRepository.Itineraries.Add(itinerary);

        var service = CreateService(
            new FakeCurrentUserAccessor { CurrentUserId = ava.Id },
            userRepository,
            itineraryRepository);

        var exception = await Assert.ThrowsAsync<BadRequestException>(() => service.ReplaceMembersAsync(itinerary.Id, itinerary.ConcurrencyToken, new ReplaceItineraryMembersRequest
        {
            UserIds = [ava.Id, mina.Id]
        }));

        Assert.Equal("New contributors must join with the itinerary share code.", exception.Message);
    }

    [Fact]
    public async Task ReplaceMembersAsync_WithUnknownUser_ThrowsBadRequestException()
    {
        var ava = TestDataFactory.CreateUser("user-ava", "Ava Santos", "ava@example.com");
        var itinerary = TestDataFactory.CreateItinerary("itinerary-tokyo", ava.Id);
        itinerary.Members.Add(TestDataFactory.CreateMember(itinerary, ava));

        var userRepository = new FakeUserRepository();
        userRepository.Users.Add(ava);
        var itineraryRepository = new FakeItineraryRepository();
        itineraryRepository.Itineraries.Add(itinerary);

        var service = CreateService(
            new FakeCurrentUserAccessor { CurrentUserId = ava.Id },
            userRepository,
            itineraryRepository);

        await Assert.ThrowsAsync<BadRequestException>(() => service.ReplaceMembersAsync(itinerary.Id, itinerary.ConcurrencyToken, new ReplaceItineraryMembersRequest
        {
            UserIds = ["user-missing"]
        }));
    }

    [Fact]
    public async Task ReplaceMembersAsync_WithStaleExpectedVersion_ThrowsPreconditionFailedException()
    {
        var ava = TestDataFactory.CreateUser("user-ava", "Ava Santos", "ava@example.com");
        var itinerary = TestDataFactory.CreateItinerary("itinerary-tokyo", ava.Id);
        itinerary.Members.Add(TestDataFactory.CreateMember(itinerary, ava));

        var userRepository = new FakeUserRepository();
        userRepository.Users.Add(ava);
        var itineraryRepository = new FakeItineraryRepository();
        itineraryRepository.Itineraries.Add(itinerary);

        var service = CreateService(
            new FakeCurrentUserAccessor { CurrentUserId = ava.Id },
            userRepository,
            itineraryRepository);

        await Assert.ThrowsAsync<PreconditionFailedException>(() => service.ReplaceMembersAsync(itinerary.Id, "stale-version", new ReplaceItineraryMembersRequest
        {
            UserIds = [ava.Id]
        }));
    }

    [Fact]
    public async Task RemoveMemberAsync_WhenOwnerRemovesContributor_RemovesMemberAndNotifies()
    {
        var ava = TestDataFactory.CreateUser("user-ava", "Ava Santos", "ava@example.com");
        var luca = TestDataFactory.CreateUser("user-luca", "Luca Reyes", "luca@example.com");
        var itinerary = TestDataFactory.CreateItinerary("itinerary-tokyo", ava.Id);
        itinerary.Members.Add(TestDataFactory.CreateMember(itinerary, ava));
        itinerary.Members.Add(TestDataFactory.CreateMember(itinerary, luca));

        var userRepository = new FakeUserRepository();
        userRepository.Users.AddRange([ava, luca]);
        var itineraryRepository = new FakeItineraryRepository();
        itineraryRepository.Itineraries.Add(itinerary);
        var notifier = new FakeRealtimeNotifier();
        var userRealtimeNotifier = new FakeUserRealtimeNotifier();
        var unitOfWork = new FakeUnitOfWork();
        var notificationRepository = new FakeUserNotificationRepository();

        var service = CreateService(
            new FakeCurrentUserAccessor { CurrentUserId = ava.Id },
            userRepository,
            itineraryRepository,
            notificationRepository,
            unitOfWork,
            notifier,
            userRealtimeNotifier);

        var originalVersion = itinerary.ConcurrencyToken;
        var response = await service.RemoveMemberAsync(itinerary.Id, luca.Id, originalVersion);

        Assert.DoesNotContain(response, member => member.UserId == luca.Id);
        Assert.Single(response, member => member.UserId == ava.Id);
        Assert.Single(notifier.Notifications);
        Assert.Equal("itinerary.members.updated", notifier.Notifications[0].Type);
        Assert.NotEqual(originalVersion, itinerary.ConcurrencyToken);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
        Assert.Single(notificationRepository.Notifications);
        Assert.Equal("itinerary.member.removed", notificationRepository.Notifications[0].Type);
        Assert.Single(userRealtimeNotifier.Notifications);
        Assert.Equal(luca.Id, userRealtimeNotifier.Notifications[0].UserId);
    }

    [Fact]
    public async Task RemoveMemberAsync_WhenRemovingCreator_ThrowsBadRequestException()
    {
        var ava = TestDataFactory.CreateUser("user-ava", "Ava Santos", "ava@example.com");
        var luca = TestDataFactory.CreateUser("user-luca", "Luca Reyes", "luca@example.com");
        var itinerary = TestDataFactory.CreateItinerary("itinerary-tokyo", ava.Id);
        itinerary.Members.Add(TestDataFactory.CreateMember(itinerary, ava));
        itinerary.Members.Add(TestDataFactory.CreateMember(itinerary, luca));

        var userRepository = new FakeUserRepository();
        userRepository.Users.AddRange([ava, luca]);
        var itineraryRepository = new FakeItineraryRepository();
        itineraryRepository.Itineraries.Add(itinerary);

        var service = CreateService(
            new FakeCurrentUserAccessor { CurrentUserId = ava.Id },
            userRepository,
            itineraryRepository);

        await Assert.ThrowsAsync<BadRequestException>(() => service.RemoveMemberAsync(itinerary.Id, ava.Id, itinerary.ConcurrencyToken));
    }

    [Fact]
    public async Task RemoveMemberAsync_WhenTargetUserIsNotMember_ThrowsNotFoundException()
    {
        var ava = TestDataFactory.CreateUser("user-ava", "Ava Santos", "ava@example.com");
        var luca = TestDataFactory.CreateUser("user-luca", "Luca Reyes", "luca@example.com");
        var mina = TestDataFactory.CreateUser("user-mina", "Mina Park", "mina@example.com");
        var itinerary = TestDataFactory.CreateItinerary("itinerary-tokyo", ava.Id);
        itinerary.Members.Add(TestDataFactory.CreateMember(itinerary, ava));
        itinerary.Members.Add(TestDataFactory.CreateMember(itinerary, luca));

        var userRepository = new FakeUserRepository();
        userRepository.Users.AddRange([ava, luca, mina]);
        var itineraryRepository = new FakeItineraryRepository();
        itineraryRepository.Itineraries.Add(itinerary);

        var service = CreateService(
            new FakeCurrentUserAccessor { CurrentUserId = ava.Id },
            userRepository,
            itineraryRepository);

        await Assert.ThrowsAsync<NotFoundException>(() => service.RemoveMemberAsync(itinerary.Id, mina.Id, itinerary.ConcurrencyToken));
    }

    private static ItineraryService CreateService(
        FakeCurrentUserAccessor currentUserAccessor,
        FakeUserRepository userRepository,
        FakeItineraryRepository itineraryRepository,
        FakeUnitOfWork? unitOfWork = null,
        FakeRealtimeNotifier? notifier = null)
    {
        return CreateService(
            currentUserAccessor,
            userRepository,
            itineraryRepository,
            new FakeUserNotificationRepository(),
            unitOfWork ?? new FakeUnitOfWork(),
            notifier ?? new FakeRealtimeNotifier(),
            new FakeUserRealtimeNotifier());
    }

    private static ItineraryService CreateService(
        FakeCurrentUserAccessor currentUserAccessor,
        FakeUserRepository userRepository,
        FakeItineraryRepository itineraryRepository,
        FakeUserNotificationRepository notificationRepository,
        FakeUnitOfWork unitOfWork,
        FakeRealtimeNotifier realtimeNotifier,
        FakeUserRealtimeNotifier userRealtimeNotifier)
    {
        return new ItineraryService(
            currentUserAccessor,
            userRepository,
            itineraryRepository,
            notificationRepository,
            unitOfWork,
            realtimeNotifier,
            userRealtimeNotifier);
    }
}
