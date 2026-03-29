using TravelPlannerApp.Application.Common.Exceptions;
using TravelPlannerApp.Application.Common.Utilities;
using TravelPlannerApp.Application.Contracts.Events;
using TravelPlannerApp.Application.Contracts.Users;
using TravelPlannerApp.Application.Services;
using TravelPlannerApp.Application.Tests.Support;
using TravelPlannerApp.Domain.Entities;
using TravelPlannerApp.Domain.Enums;

namespace TravelPlannerApp.Application.Tests.Common;

public sealed class UtilityAndEdgeCaseTests
{
    [Fact]
    public void AvatarGenerator_WhenNameIsBlankOrSingleWord_ReturnsExpectedInitials()
    {
        Assert.Equal("?", AvatarGenerator.Generate(" "));
        Assert.Equal("AV", AvatarGenerator.Generate("Ava"));
    }

    [Fact]
    public void TimeZoneHelper_WhenValidAndInvalidTimezones_ReturnsOffsetOrThrows()
    {
        var offset = TimeZoneHelper.ToOffset(new DateTime(2026, 4, 15, 10, 0, 0), "Asia/Tokyo");

        Assert.Equal(9, offset.Offset.TotalHours);
        Assert.Throws<BadRequestException>(() => TimeZoneHelper.EnsureExists("Invalid/Timezone"));
    }

    [Fact]
    public void TimeZoneHelper_Utc_NormalizesKinds()
    {
        var utcValue = TimeZoneHelper.Utc(new DateTime(2026, 4, 15, 10, 0, 0, DateTimeKind.Utc));
        var unspecified = TimeZoneHelper.Utc(new DateTime(2026, 4, 15, 10, 0, 0, DateTimeKind.Unspecified));

        Assert.Equal(DateTimeKind.Utc, utcValue.Kind);
        Assert.Equal(DateTimeKind.Utc, unspecified.Kind);
    }

    [Fact]
    public void IdGenerator_New_IncludesPrefix()
    {
        var id = IdGenerator.New("user");

        Assert.StartsWith("user-", id, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UserService_UpdateUserAsync_WithoutAvatar_GeneratesAvatar()
    {
        var userRepository = new FakeUserRepository();
        userRepository.Users.Add(TestDataFactory.CreateUser("user-ava", "Ava Santos", "ava@example.com", "XX"));
        var service = new UserService(userRepository, new FakeUnitOfWork());

        var response = await service.UpdateUserAsync("user-ava", "user-ava-v1", new UpdateUserRequest
        {
            Name = "Ava Santos",
            Email = "ava@example.com",
            Avatar = " "
        });

        Assert.Equal("AS", response.Avatar);
    }

    [Fact]
    public async Task ItineraryService_GetMembersAsync_ReturnsSortedMembers()
    {
        var ava = TestDataFactory.CreateUser("user-ava", "Ava Santos", "ava@example.com");
        var luca = TestDataFactory.CreateUser("user-luca", "Luca Reyes", "luca@example.com");
        var itinerary = TestDataFactory.CreateItinerary("itinerary-tokyo", ava.Id);
        itinerary.Members.Add(TestDataFactory.CreateMember(itinerary, luca));
        itinerary.Members.Add(TestDataFactory.CreateMember(itinerary, ava));

        var userRepository = new FakeUserRepository();
        userRepository.Users.AddRange([ava, luca]);
        var itineraryRepository = new FakeItineraryRepository();
        itineraryRepository.Itineraries.Add(itinerary);
        var service = new ItineraryService(
            new FakeCurrentUserAccessor { CurrentUserId = ava.Id },
            userRepository,
            itineraryRepository,
            new FakeUnitOfWork(),
            new FakeRealtimeNotifier());

        var members = await service.GetMembersAsync(itinerary.Id);

        Assert.Equal(["user-ava", "user-luca"], members.Select(member => member.UserId).ToArray());
    }

    [Fact]
    public async Task ItineraryService_GetItineraryById_WhenMissing_ThrowsNotFound()
    {
        var ava = TestDataFactory.CreateUser("user-ava", "Ava Santos", "ava@example.com");
        var userRepository = new FakeUserRepository();
        userRepository.Users.Add(ava);
        var service = new ItineraryService(
            new FakeCurrentUserAccessor { CurrentUserId = ava.Id },
            userRepository,
            new FakeItineraryRepository(),
            new FakeUnitOfWork(),
            new FakeRealtimeNotifier());

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetItineraryByIdAsync("missing"));
    }

    [Fact]
    public async Task EventService_GetEventById_WhenMissing_ThrowsNotFoundException()
    {
        var ava = TestDataFactory.CreateUser("user-ava", "Ava Santos", "ava@example.com");
        var userRepository = new FakeUserRepository();
        userRepository.Users.Add(ava);
        var service = new EventService(
            new FakeCurrentUserAccessor { CurrentUserId = ava.Id },
            userRepository,
            new FakeItineraryRepository(),
            new FakeEventRepository(),
            new FakeUnitOfWork(),
            new FakeRealtimeNotifier());

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetEventByIdAsync("missing"));
    }

    [Fact]
    public async Task EventService_GetEventById_WhenCurrentUserIsNotMember_ThrowsForbiddenException()
    {
        var ava = TestDataFactory.CreateUser("user-ava", "Ava Santos", "ava@example.com");
        var luca = TestDataFactory.CreateUser("user-luca", "Luca Reyes", "luca@example.com");
        var itinerary = TestDataFactory.CreateItinerary("itinerary-tokyo", ava.Id);
        itinerary.Members.Add(TestDataFactory.CreateMember(itinerary, ava));
        var eventEntity = TestDataFactory.CreateEvent("evt-1", itinerary.Id, ava.Id, ava.Id);

        var userRepository = new FakeUserRepository();
        userRepository.Users.AddRange([ava, luca]);
        var itineraryRepository = new FakeItineraryRepository();
        itineraryRepository.Itineraries.Add(itinerary);
        var eventRepository = new FakeEventRepository();
        eventRepository.Events.Add(eventEntity);
        var service = new EventService(
            new FakeCurrentUserAccessor { CurrentUserId = luca.Id },
            userRepository,
            itineraryRepository,
            eventRepository,
            new FakeUnitOfWork(),
            new FakeRealtimeNotifier());

        await Assert.ThrowsAsync<ForbiddenException>(() => service.GetEventByIdAsync(eventEntity.Id));
    }

    [Fact]
    public async Task EventService_GetHistoryAsync_WhenUnknownEvent_ThrowsNotFoundException()
    {
        var ava = TestDataFactory.CreateUser("user-ava", "Ava Santos", "ava@example.com");
        var userRepository = new FakeUserRepository();
        userRepository.Users.Add(ava);
        var service = new EventService(
            new FakeCurrentUserAccessor { CurrentUserId = ava.Id },
            userRepository,
            new FakeItineraryRepository(),
            new FakeEventRepository(),
            new FakeUnitOfWork(),
            new FakeRealtimeNotifier());

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetHistoryAsync("missing"));
    }

    [Fact]
    public async Task EventService_GetEventsAsync_WithoutCurrentUser_ThrowsBadRequestException()
    {
        var service = new EventService(
            new FakeCurrentUserAccessor(),
            new FakeUserRepository(),
            new FakeItineraryRepository(),
            new FakeEventRepository(),
            new FakeUnitOfWork(),
            new FakeRealtimeNotifier());

        await Assert.ThrowsAsync<BadRequestException>(() => service.GetEventsAsync("itinerary-tokyo"));
    }

    [Fact]
    public async Task EventService_UpdateEventAsync_WhenDetailsChange_WritesUpdatedAuditLog()
    {
        var ava = TestDataFactory.CreateUser("user-ava", "Ava Santos", "ava@example.com");
        var itinerary = TestDataFactory.CreateItinerary("itinerary-tokyo", ava.Id);
        itinerary.Members.Add(TestDataFactory.CreateMember(itinerary, ava));
        var eventEntity = TestDataFactory.CreateEvent("evt-1", itinerary.Id, ava.Id, ava.Id, "Dinner");

        var userRepository = new FakeUserRepository();
        userRepository.Users.Add(ava);
        var itineraryRepository = new FakeItineraryRepository();
        itineraryRepository.Itineraries.Add(itinerary);
        var eventRepository = new FakeEventRepository();
        eventRepository.Events.Add(eventEntity);
        var service = new EventService(
            new FakeCurrentUserAccessor { CurrentUserId = ava.Id },
            userRepository,
            itineraryRepository,
            eventRepository,
            new FakeUnitOfWork(),
            new FakeRealtimeNotifier());

        await service.UpdateEventAsync(eventEntity.Id, eventEntity.ConcurrencyToken, new UpdateEventRequest
        {
            Title = "Dinner at Ginza",
            Description = "Updated",
            Category = EventCategory.Restaurant,
            Color = "#ff0000",
            StartDateTime = eventEntity.StartDateTimeLocal,
            EndDateTime = eventEntity.EndDateTimeLocal,
            Timezone = eventEntity.Timezone,
            Location = eventEntity.Location,
            LocationAddress = eventEntity.LocationAddress,
            LocationLat = eventEntity.LocationLat,
            LocationLng = eventEntity.LocationLng,
            Cost = eventEntity.Cost
        });

        var audit = Assert.Single(eventRepository.AuditLogs);
        Assert.Equal("Updated event 'Dinner at Ginza'.", audit.Summary);
    }
}
