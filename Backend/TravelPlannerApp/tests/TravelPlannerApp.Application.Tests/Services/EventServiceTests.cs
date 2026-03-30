using TravelPlannerApp.Application.Common.Exceptions;
using TravelPlannerApp.Application.Contracts.Events;
using TravelPlannerApp.Application.Services;
using TravelPlannerApp.Application.Tests.Support;
using TravelPlannerApp.Domain.Entities;
using TravelPlannerApp.Domain.Enums;

namespace TravelPlannerApp.Application.Tests.Services;

public sealed class EventServiceTests
{
    [Fact]
    public async Task GetEventsAsync_ReturnsEventsSortedByStartTime()
    {
        var ava = TestDataFactory.CreateUser("user-ava", "Ava Santos", "ava@example.com");
        var itinerary = TestDataFactory.CreateItinerary("itinerary-tokyo", ava.Id);
        itinerary.Members.Add(TestDataFactory.CreateMember(itinerary, ava));

        var later = TestDataFactory.CreateEvent("evt-2", itinerary.Id, ava.Id, ava.Id, "Later");
        later.StartDateTimeLocal = new DateTime(2026, 4, 15, 16, 0, 0, DateTimeKind.Unspecified);
        later.EndDateTimeLocal = new DateTime(2026, 4, 15, 18, 0, 0, DateTimeKind.Unspecified);

        var earlier = TestDataFactory.CreateEvent("evt-1", itinerary.Id, ava.Id, ava.Id, "Earlier");
        earlier.StartDateTimeLocal = new DateTime(2026, 4, 15, 8, 0, 0, DateTimeKind.Unspecified);
        earlier.EndDateTimeLocal = new DateTime(2026, 4, 15, 9, 0, 0, DateTimeKind.Unspecified);

        var service = CreateService(ava, [ava], [itinerary], [later, earlier]);

        var response = await service.GetEventsAsync(itinerary.Id);

        Assert.Equal(["evt-1", "evt-2"], response.Select(eventEntity => eventEntity.Id).ToArray());
    }

    [Fact]
    public async Task CreateEventAsync_WithInvalidTimezone_ThrowsBadRequestException()
    {
        var ava = TestDataFactory.CreateUser("user-ava", "Ava Santos", "ava@example.com");
        var itinerary = TestDataFactory.CreateItinerary("itinerary-tokyo", ava.Id);
        itinerary.Members.Add(TestDataFactory.CreateMember(itinerary, ava));

        var service = CreateService(ava, [ava], [itinerary], []);

        await Assert.ThrowsAsync<BadRequestException>(() => service.CreateEventAsync(itinerary.Id, new CreateEventRequest
        {
            Title = "Dinner",
            Category = EventCategory.Restaurant,
            StartDateTime = new DateTime(2026, 4, 15, 18, 0, 0),
            EndDateTime = new DateTime(2026, 4, 15, 20, 0, 0),
            Timezone = "Mars/Olympus"
        }));
    }

    [Fact]
    public async Task CreateEventAsync_WhenCurrentUserIsNotMember_ThrowsForbiddenException()
    {
        var ava = TestDataFactory.CreateUser("user-ava", "Ava Santos", "ava@example.com");
        var luca = TestDataFactory.CreateUser("user-luca", "Luca Reyes", "luca@example.com");
        var itinerary = TestDataFactory.CreateItinerary("itinerary-tokyo", ava.Id);
        itinerary.Members.Add(TestDataFactory.CreateMember(itinerary, ava));

        var service = CreateService(luca, [ava, luca], [itinerary], []);

        await Assert.ThrowsAsync<ForbiddenException>(() => service.CreateEventAsync(itinerary.Id, new CreateEventRequest
        {
            Title = "Dinner",
            Category = EventCategory.Restaurant,
            StartDateTime = new DateTime(2026, 4, 15, 18, 0, 0),
            EndDateTime = new DateTime(2026, 4, 15, 20, 0, 0),
            Timezone = "Asia/Tokyo"
        }));
    }

    [Fact]
    public async Task CreateEventAsync_WithValidRequest_AddsEventAuditAndNotification()
    {
        var ava = TestDataFactory.CreateUser("user-ava", "Ava Santos", "ava@example.com");
        var itinerary = TestDataFactory.CreateItinerary("itinerary-tokyo", ava.Id);
        itinerary.Members.Add(TestDataFactory.CreateMember(itinerary, ava));

        var eventRepository = new FakeEventRepository();
        var notifier = new FakeRealtimeNotifier();
        var service = CreateService(ava, [ava], [itinerary], [], eventRepository, notifier);

        var response = await service.CreateEventAsync(itinerary.Id, new CreateEventRequest
        {
            Title = "Shibuya Walk",
            Category = EventCategory.Activity,
            StartDateTime = new DateTime(2026, 4, 15, 18, 0, 0),
            EndDateTime = new DateTime(2026, 4, 15, 20, 0, 0),
            Timezone = "Asia/Tokyo",
            Cost = 20m,
            CurrencyCode = "JPY"
        });

        Assert.Equal("Shibuya Walk", response.Title);
        Assert.Equal("JPY", response.CurrencyCode);
        Assert.False(string.IsNullOrWhiteSpace(response.Version));
        Assert.Single(eventRepository.Events);
        Assert.Single(eventRepository.AuditLogs);
        Assert.Equal(EventAuditAction.Created, eventRepository.AuditLogs[0].Action);
        Assert.Single(notifier.Notifications);
        Assert.Equal("event.created", notifier.Notifications[0].Type);
    }

    [Fact]
    public async Task UpdateEventAsync_WhenOnlyScheduleChanges_WritesRescheduledAuditLog()
    {
        var ava = TestDataFactory.CreateUser("user-ava", "Ava Santos", "ava@example.com");
        var itinerary = TestDataFactory.CreateItinerary("itinerary-tokyo", ava.Id);
        itinerary.Members.Add(TestDataFactory.CreateMember(itinerary, ava));
        var existingEvent = TestDataFactory.CreateEvent("evt-1", itinerary.Id, ava.Id, ava.Id, "Dinner");

        var eventRepository = new FakeEventRepository();
        eventRepository.Events.Add(existingEvent);
        var service = CreateService(ava, [ava], [itinerary], [existingEvent], eventRepository);

        var originalVersion = existingEvent.ConcurrencyToken;
        await service.UpdateEventAsync(existingEvent.Id, originalVersion, new UpdateEventRequest
        {
            Title = "Dinner",
            Description = existingEvent.Description,
            Category = existingEvent.Category,
            Color = existingEvent.Color,
            StartDateTime = existingEvent.StartDateTimeLocal.AddHours(1),
            EndDateTime = existingEvent.EndDateTimeLocal.AddHours(1),
            Timezone = existingEvent.Timezone,
            Location = existingEvent.Location,
            LocationAddress = existingEvent.LocationAddress,
            LocationLat = existingEvent.LocationLat,
            LocationLng = existingEvent.LocationLng,
            Cost = existingEvent.Cost,
            CurrencyCode = existingEvent.CurrencyCode
        });

        var audit = Assert.Single(eventRepository.AuditLogs);
        Assert.Equal("Rescheduled event 'Dinner'.", audit.Summary);
        Assert.NotEqual(originalVersion, existingEvent.ConcurrencyToken);
    }

    [Fact]
    public async Task GetHistoryAsync_WhenEventWasDeleted_ReturnsAuditHistoryFromLogs()
    {
        var ava = TestDataFactory.CreateUser("user-ava", "Ava Santos", "ava@example.com");
        var itinerary = TestDataFactory.CreateItinerary("itinerary-tokyo", ava.Id);
        itinerary.Members.Add(TestDataFactory.CreateMember(itinerary, ava));
        var deletedEvent = TestDataFactory.CreateEvent("evt-deleted", itinerary.Id, ava.Id, ava.Id, "Deleted");

        var eventRepository = new FakeEventRepository();
        eventRepository.AuditLogs.Add(TestDataFactory.CreateAuditLog(deletedEvent, EventAuditAction.Created, "Created event 'Deleted'."));
        eventRepository.AuditLogs.Add(TestDataFactory.CreateAuditLog(deletedEvent, EventAuditAction.Deleted, "Deleted event 'Deleted'."));

        var service = CreateService(ava, [ava], [itinerary], [], eventRepository);

        var history = await service.GetHistoryAsync(deletedEvent.Id);

        Assert.Equal(2, history.Count);
        Assert.Contains(history, log => log.Action == EventAuditAction.Deleted);
    }

    [Fact]
    public async Task DeleteEventAsync_RemovesEventAndWritesDeletedAuditLog()
    {
        var ava = TestDataFactory.CreateUser("user-ava", "Ava Santos", "ava@example.com");
        var itinerary = TestDataFactory.CreateItinerary("itinerary-tokyo", ava.Id);
        itinerary.Members.Add(TestDataFactory.CreateMember(itinerary, ava));
        var existingEvent = TestDataFactory.CreateEvent("evt-1", itinerary.Id, ava.Id, ava.Id, "Dinner");

        var eventRepository = new FakeEventRepository();
        eventRepository.Events.Add(existingEvent);
        var notifier = new FakeRealtimeNotifier();
        var service = CreateService(ava, [ava], [itinerary], [existingEvent], eventRepository, notifier);

        await service.DeleteEventAsync(existingEvent.Id, existingEvent.ConcurrencyToken);

        Assert.Empty(eventRepository.Events);
        Assert.Single(eventRepository.AuditLogs);
        Assert.Equal(EventAuditAction.Deleted, eventRepository.AuditLogs[0].Action);
        Assert.Single(notifier.Notifications);
        Assert.Equal("event.deleted", notifier.Notifications[0].Type);
    }

    [Fact]
    public async Task DeleteEventAsync_WhenCurrentUserOwnsItinerary_CanDeleteAnotherUsersEvent()
    {
        var ava = TestDataFactory.CreateUser("user-ava", "Ava Santos", "ava@example.com");
        var luca = TestDataFactory.CreateUser("user-luca", "Luca Reyes", "luca@example.com");
        var itinerary = TestDataFactory.CreateItinerary("itinerary-tokyo", ava.Id);
        itinerary.Members.Add(TestDataFactory.CreateMember(itinerary, ava));
        itinerary.Members.Add(TestDataFactory.CreateMember(itinerary, luca));
        var existingEvent = TestDataFactory.CreateEvent("evt-1", itinerary.Id, luca.Id, luca.Id, "Dinner");

        var eventRepository = new FakeEventRepository();
        eventRepository.Events.Add(existingEvent);
        var service = CreateService(ava, [ava, luca], [itinerary], [existingEvent], eventRepository);

        await service.DeleteEventAsync(existingEvent.Id, existingEvent.ConcurrencyToken);

        Assert.Empty(eventRepository.Events);
        Assert.Single(eventRepository.AuditLogs);
        Assert.Equal(EventAuditAction.Deleted, eventRepository.AuditLogs[0].Action);
    }

    [Fact]
    public async Task DeleteEventAsync_WhenCurrentUserIsMemberButNotEventCreatorOrItineraryOwner_ThrowsForbiddenException()
    {
        var ava = TestDataFactory.CreateUser("user-ava", "Ava Santos", "ava@example.com");
        var luca = TestDataFactory.CreateUser("user-luca", "Luca Reyes", "luca@example.com");
        var mina = TestDataFactory.CreateUser("user-mina", "Mina Park", "mina@example.com");
        var itinerary = TestDataFactory.CreateItinerary("itinerary-tokyo", ava.Id);
        itinerary.Members.Add(TestDataFactory.CreateMember(itinerary, ava));
        itinerary.Members.Add(TestDataFactory.CreateMember(itinerary, luca));
        itinerary.Members.Add(TestDataFactory.CreateMember(itinerary, mina));
        var existingEvent = TestDataFactory.CreateEvent("evt-1", itinerary.Id, ava.Id, ava.Id, "Dinner");

        var eventRepository = new FakeEventRepository();
        eventRepository.Events.Add(existingEvent);
        var service = CreateService(luca, [ava, luca, mina], [itinerary], [existingEvent], eventRepository);

        await Assert.ThrowsAsync<ForbiddenException>(() => service.DeleteEventAsync(existingEvent.Id, existingEvent.ConcurrencyToken));
    }

    [Fact]
    public async Task UpdateEventAsync_WithoutExpectedVersion_ThrowsPreconditionRequiredException()
    {
        var ava = TestDataFactory.CreateUser("user-ava", "Ava Santos", "ava@example.com");
        var itinerary = TestDataFactory.CreateItinerary("itinerary-tokyo", ava.Id);
        itinerary.Members.Add(TestDataFactory.CreateMember(itinerary, ava));
        var existingEvent = TestDataFactory.CreateEvent("evt-1", itinerary.Id, ava.Id, ava.Id, "Dinner");

        var eventRepository = new FakeEventRepository();
        eventRepository.Events.Add(existingEvent);
        var service = CreateService(ava, [ava], [itinerary], [existingEvent], eventRepository);

        await Assert.ThrowsAsync<PreconditionRequiredException>(() => service.UpdateEventAsync(existingEvent.Id, null, new UpdateEventRequest
        {
            Title = "Dinner",
            Description = existingEvent.Description,
            Category = existingEvent.Category,
            Color = existingEvent.Color,
            StartDateTime = existingEvent.StartDateTimeLocal,
            EndDateTime = existingEvent.EndDateTimeLocal,
            Timezone = existingEvent.Timezone,
            Location = existingEvent.Location,
            LocationAddress = existingEvent.LocationAddress,
            LocationLat = existingEvent.LocationLat,
            LocationLng = existingEvent.LocationLng,
            Cost = existingEvent.Cost,
            CurrencyCode = existingEvent.CurrencyCode
        }));
    }

    [Fact]
    public async Task DeleteEventAsync_WithStaleExpectedVersion_ThrowsPreconditionFailedException()
    {
        var ava = TestDataFactory.CreateUser("user-ava", "Ava Santos", "ava@example.com");
        var itinerary = TestDataFactory.CreateItinerary("itinerary-tokyo", ava.Id);
        itinerary.Members.Add(TestDataFactory.CreateMember(itinerary, ava));
        var existingEvent = TestDataFactory.CreateEvent("evt-1", itinerary.Id, ava.Id, ava.Id, "Dinner");

        var eventRepository = new FakeEventRepository();
        eventRepository.Events.Add(existingEvent);
        var service = CreateService(ava, [ava], [itinerary], [existingEvent], eventRepository);

        await Assert.ThrowsAsync<PreconditionFailedException>(() => service.DeleteEventAsync(existingEvent.Id, "stale-version"));
    }

    private static EventService CreateService(
        User currentUser,
        IEnumerable<User> users,
        IEnumerable<Itinerary> itineraries,
        IEnumerable<Event> events,
        FakeEventRepository? eventRepository = null,
        FakeRealtimeNotifier? notifier = null)
    {
        var userRepository = new FakeUserRepository();
        userRepository.Users.AddRange(users);

        var itineraryRepository = new FakeItineraryRepository();
        itineraryRepository.Itineraries.AddRange(itineraries);

        eventRepository ??= new FakeEventRepository();
        if (!ReferenceEquals(events, eventRepository.Events))
        {
            eventRepository.Events.AddRange(events.Where(eventEntity => eventRepository.Events.All(existing => existing.Id != eventEntity.Id)));
        }

        return new EventService(
            new FakeCurrentUserAccessor { CurrentUserId = currentUser.Id },
            userRepository,
            itineraryRepository,
            eventRepository,
            new FakeUnitOfWork(),
            notifier ?? new FakeRealtimeNotifier());
    }
}
