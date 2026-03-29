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
        var earlierTrip = TestDataFactory.CreateItinerary("itinerary-a", ava.Id, "Alpha", new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 5));
        earlierTrip.Members.Add(TestDataFactory.CreateMember(earlierTrip, ava));
        repo.Itineraries.AddRange([laterTrip, earlierTrip]);

        var service = new ItineraryService(currentUser, userRepository, repo, new FakeUnitOfWork(), new FakeRealtimeNotifier());

        var response = await service.GetAccessibleItinerariesAsync();

        Assert.Equal(["itinerary-a", "itinerary-b"], response.Select(itinerary => itinerary.Id).ToArray());
    }

    [Fact]
    public async Task GetAccessibleItinerariesAsync_WithoutCurrentUser_ThrowsBadRequestException()
    {
        var service = new ItineraryService(
            new FakeCurrentUserAccessor(),
            new FakeUserRepository(),
            new FakeItineraryRepository(),
            new FakeUnitOfWork(),
            new FakeRealtimeNotifier());

        await Assert.ThrowsAsync<BadRequestException>(() => service.GetAccessibleItinerariesAsync());
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

        var service = new ItineraryService(
            new FakeCurrentUserAccessor { CurrentUserId = luca.Id },
            userRepository,
            itineraryRepository,
            new FakeUnitOfWork(),
            new FakeRealtimeNotifier());

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

        var service = new ItineraryService(
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

        var service = new ItineraryService(
            new FakeCurrentUserAccessor { CurrentUserId = ava.Id },
            userRepository,
            itineraryRepository,
            new FakeUnitOfWork(),
            notifier);

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

        var service = new ItineraryService(
            new FakeCurrentUserAccessor { CurrentUserId = ava.Id },
            userRepository,
            itineraryRepository,
            new FakeUnitOfWork(),
            notifier);

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
    public async Task ReplaceMembersAsync_WithUnknownUser_ThrowsBadRequestException()
    {
        var ava = TestDataFactory.CreateUser("user-ava", "Ava Santos", "ava@example.com");
        var itinerary = TestDataFactory.CreateItinerary("itinerary-tokyo", ava.Id);
        itinerary.Members.Add(TestDataFactory.CreateMember(itinerary, ava));

        var userRepository = new FakeUserRepository();
        userRepository.Users.Add(ava);
        var itineraryRepository = new FakeItineraryRepository();
        itineraryRepository.Itineraries.Add(itinerary);

        var service = new ItineraryService(
            new FakeCurrentUserAccessor { CurrentUserId = ava.Id },
            userRepository,
            itineraryRepository,
            new FakeUnitOfWork(),
            new FakeRealtimeNotifier());

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

        var service = new ItineraryService(
            new FakeCurrentUserAccessor { CurrentUserId = ava.Id },
            userRepository,
            itineraryRepository,
            new FakeUnitOfWork(),
            new FakeRealtimeNotifier());

        await Assert.ThrowsAsync<PreconditionFailedException>(() => service.ReplaceMembersAsync(itinerary.Id, "stale-version", new ReplaceItineraryMembersRequest
        {
            UserIds = [ava.Id]
        }));
    }
}
