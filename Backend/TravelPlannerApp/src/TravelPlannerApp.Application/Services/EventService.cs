using System.Text.Json;
using TravelPlannerApp.Application.Abstractions.CurrentUser;
using TravelPlannerApp.Application.Abstractions.Persistence;
using TravelPlannerApp.Application.Abstractions.Realtime;
using TravelPlannerApp.Application.Common.Exceptions;
using TravelPlannerApp.Application.Common.Utilities;
using TravelPlannerApp.Application.Contracts.Events;
using TravelPlannerApp.Application.Mappings;
using TravelPlannerApp.Domain.Entities;
using TravelPlannerApp.Domain.Enums;

namespace TravelPlannerApp.Application.Services;

public sealed class EventService : IEventService
{
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly IUserRepository _userRepository;
    private readonly IItineraryRepository _itineraryRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IItineraryRealtimeNotifier _notifier;

    public EventService(
        ICurrentUserAccessor currentUserAccessor,
        IUserRepository userRepository,
        IItineraryRepository itineraryRepository,
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        IItineraryRealtimeNotifier notifier)
    {
        _currentUserAccessor = currentUserAccessor;
        _userRepository = userRepository;
        _itineraryRepository = itineraryRepository;
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _notifier = notifier;
    }

    public async Task<IReadOnlyList<EventResponse>> GetEventsAsync(string itineraryId, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        await EnsureItineraryAccessAsync(itineraryId, currentUser.Id, cancellationToken);

        var events = await _eventRepository.ListByItineraryAsync(itineraryId, cancellationToken);
        return events
            .OrderBy(static eventEntity => eventEntity.StartDateTimeLocal)
            .ThenBy(static eventEntity => eventEntity.Title, StringComparer.OrdinalIgnoreCase)
            .Select(static eventEntity => eventEntity.ToResponse())
            .ToList();
    }

    public async Task<EventResponse> GetEventByIdAsync(string eventId, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        var eventEntity = await _eventRepository.GetByIdAsync(eventId, cancellationToken)
            ?? throw new NotFoundException($"Event '{eventId}' was not found.");

        await EnsureItineraryAccessAsync(eventEntity.ItineraryId, currentUser.Id, cancellationToken);
        return eventEntity.ToResponse();
    }

    public async Task<EventResponse> CreateEventAsync(string itineraryId, CreateEventRequest request, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        await EnsureItineraryAccessAsync(itineraryId, currentUser.Id, cancellationToken);
        TimeZoneHelper.EnsureExists(request.Timezone);

        var now = DateTime.UtcNow;
        var eventEntity = new Event
        {
            Id = IdGenerator.New("evt"),
            ConcurrencyToken = ConcurrencyTokenHelper.NewToken(),
            ItineraryId = itineraryId,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            Category = request.Category,
            Color = request.Color?.Trim(),
            StartDateTimeLocal = DateTime.SpecifyKind(request.StartDateTime, DateTimeKind.Unspecified),
            EndDateTimeLocal = DateTime.SpecifyKind(request.EndDateTime, DateTimeKind.Unspecified),
            Timezone = request.Timezone.Trim(),
            Location = request.Location?.Trim(),
            LocationAddress = request.LocationAddress?.Trim(),
            LocationLat = request.LocationLat,
            LocationLng = request.LocationLng,
            Cost = request.Cost,
            CreatedById = currentUser.Id,
            UpdatedById = currentUser.Id,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        await _eventRepository.AddAsync(eventEntity, cancellationToken);
        await _eventRepository.AddAuditLogAsync(
            CreateAuditLog(eventEntity, EventAuditAction.Created, $"Created event '{eventEntity.Title}'.", currentUser.Id, now),
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _notifier.NotifyAsync(
            itineraryId,
            new ItineraryRealtimeNotification("event.created", itineraryId, eventEntity.Id, now, eventEntity.ToResponse()),
            cancellationToken);

        return eventEntity.ToResponse();
    }

    public async Task<EventResponse> UpdateEventAsync(string eventId, string? expectedVersion, UpdateEventRequest request, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        var eventEntity = await _eventRepository.GetByIdAsync(eventId, cancellationToken)
            ?? throw new NotFoundException($"Event '{eventId}' was not found.");

        await EnsureItineraryAccessAsync(eventEntity.ItineraryId, currentUser.Id, cancellationToken);
        ConcurrencyTokenHelper.EnsureMatches(eventEntity.ConcurrencyToken, expectedVersion);
        TimeZoneHelper.EnsureExists(request.Timezone);

        var summary = BuildUpdateSummary(eventEntity, request);
        eventEntity.Title = request.Title.Trim();
        eventEntity.Description = request.Description?.Trim();
        eventEntity.Category = request.Category;
        eventEntity.Color = request.Color?.Trim();
        eventEntity.StartDateTimeLocal = DateTime.SpecifyKind(request.StartDateTime, DateTimeKind.Unspecified);
        eventEntity.EndDateTimeLocal = DateTime.SpecifyKind(request.EndDateTime, DateTimeKind.Unspecified);
        eventEntity.Timezone = request.Timezone.Trim();
        eventEntity.Location = request.Location?.Trim();
        eventEntity.LocationAddress = request.LocationAddress?.Trim();
        eventEntity.LocationLat = request.LocationLat;
        eventEntity.LocationLng = request.LocationLng;
        eventEntity.Cost = request.Cost;
        eventEntity.ConcurrencyToken = ConcurrencyTokenHelper.NewToken();
        eventEntity.UpdatedById = currentUser.Id;
        eventEntity.UpdatedAtUtc = DateTime.UtcNow;

        await _eventRepository.AddAuditLogAsync(
            CreateAuditLog(eventEntity, EventAuditAction.Updated, summary, currentUser.Id, eventEntity.UpdatedAtUtc),
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _notifier.NotifyAsync(
            eventEntity.ItineraryId,
            new ItineraryRealtimeNotification("event.updated", eventEntity.ItineraryId, eventEntity.Id, eventEntity.UpdatedAtUtc, eventEntity.ToResponse()),
            cancellationToken);

        return eventEntity.ToResponse();
    }

    public async Task DeleteEventAsync(string eventId, string? expectedVersion, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        var eventEntity = await _eventRepository.GetByIdAsync(eventId, cancellationToken)
            ?? throw new NotFoundException($"Event '{eventId}' was not found.");

        await EnsureItineraryAccessAsync(eventEntity.ItineraryId, currentUser.Id, cancellationToken);
        ConcurrencyTokenHelper.EnsureMatches(eventEntity.ConcurrencyToken, expectedVersion);
        await EnsureCanDeleteEventAsync(eventEntity, currentUser.Id, cancellationToken);

        var deletedAt = DateTime.UtcNow;
        eventEntity.UpdatedById = currentUser.Id;
        eventEntity.UpdatedAtUtc = deletedAt;

        await _eventRepository.AddAuditLogAsync(
            CreateAuditLog(eventEntity, EventAuditAction.Deleted, $"Deleted event '{eventEntity.Title}'.", currentUser.Id, deletedAt),
            cancellationToken);
        _eventRepository.Remove(eventEntity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _notifier.NotifyAsync(
            eventEntity.ItineraryId,
            new ItineraryRealtimeNotification("event.deleted", eventEntity.ItineraryId, eventEntity.Id, deletedAt, new { eventId = eventEntity.Id }),
            cancellationToken);
    }

    public async Task<IReadOnlyList<EventAuditLogResponse>> GetHistoryAsync(string eventId, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        var eventEntity = await _eventRepository.GetByIdAsync(eventId, cancellationToken);
        var itineraryId = eventEntity?.ItineraryId ?? await _eventRepository.GetHistoryItineraryIdAsync(eventId, cancellationToken);

        if (string.IsNullOrWhiteSpace(itineraryId))
        {
            throw new NotFoundException($"Event '{eventId}' was not found.");
        }

        await EnsureItineraryAccessAsync(itineraryId, currentUser.Id, cancellationToken);

        var history = await _eventRepository.ListHistoryAsync(eventId, cancellationToken);
        return history
            .OrderByDescending(static log => log.ChangedAtUtc)
            .Select(static log => log.ToResponse())
            .ToList();
    }

    private async Task<User> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserAccessor.GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            throw new UnauthorizedException("Authenticated user is required.");
        }

        var currentUser = await _userRepository.GetByIdAsync(currentUserId.Trim(), cancellationToken);
        if (currentUser is null)
        {
            throw new UnauthorizedException($"Current user '{currentUserId}' was not found.");
        }

        return currentUser;
    }

    private async Task EnsureItineraryAccessAsync(string itineraryId, string userId, CancellationToken cancellationToken)
    {
        var isMember = await _itineraryRepository.IsMemberAsync(itineraryId, userId, cancellationToken);
        if (isMember)
        {
            return;
        }

        var itinerary = await _itineraryRepository.GetByIdAsync(itineraryId, cancellationToken);
        if (itinerary is null)
        {
            throw new NotFoundException($"Itinerary '{itineraryId}' was not found.");
        }

        throw new ForbiddenException($"You do not have access to itinerary '{itineraryId}'.");
    }

    private async Task EnsureCanDeleteEventAsync(Event eventEntity, string currentUserId, CancellationToken cancellationToken)
    {
        if (string.Equals(eventEntity.CreatedById, currentUserId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var itinerary = await _itineraryRepository.GetByIdAsync(eventEntity.ItineraryId, cancellationToken)
            ?? throw new NotFoundException($"Itinerary '{eventEntity.ItineraryId}' was not found.");

        if (string.Equals(itinerary.CreatedById, currentUserId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        throw new ForbiddenException("Only the event creator or itinerary owner can delete this event.");
    }

    private static EventAuditLog CreateAuditLog(Event eventEntity, EventAuditAction action, string summary, string changedByUserId, DateTime changedAtUtc)
    {
        return new EventAuditLog
        {
            Id = IdGenerator.New("audit"),
            EventId = eventEntity.Id,
            ItineraryId = eventEntity.ItineraryId,
            Action = action,
            Summary = summary,
            SnapshotJson = JsonSerializer.Serialize(eventEntity.ToAuditSnapshot(), AuditJson.Options),
            ChangedByUserId = changedByUserId,
            ChangedAtUtc = changedAtUtc
        };
    }

    private static string BuildUpdateSummary(Event current, UpdateEventRequest request)
    {
        var normalizedTitle = request.Title.Trim();
        var normalizedTimezone = request.Timezone.Trim();
        var scheduleChanged = current.StartDateTimeLocal != DateTime.SpecifyKind(request.StartDateTime, DateTimeKind.Unspecified)
            || current.EndDateTimeLocal != DateTime.SpecifyKind(request.EndDateTime, DateTimeKind.Unspecified)
            || !string.Equals(current.Timezone, normalizedTimezone, StringComparison.OrdinalIgnoreCase);

        var detailsChanged = !string.Equals(current.Title, normalizedTitle, StringComparison.Ordinal)
            || !string.Equals(current.Description, request.Description?.Trim(), StringComparison.Ordinal)
            || current.Category != request.Category
            || !string.Equals(current.Color, request.Color?.Trim(), StringComparison.Ordinal)
            || !string.Equals(current.Location, request.Location?.Trim(), StringComparison.Ordinal)
            || !string.Equals(current.LocationAddress, request.LocationAddress?.Trim(), StringComparison.Ordinal)
            || current.LocationLat != request.LocationLat
            || current.LocationLng != request.LocationLng
            || current.Cost != request.Cost;

        if (scheduleChanged && !detailsChanged)
        {
            return $"Rescheduled event '{normalizedTitle}'.";
        }

        return $"Updated event '{normalizedTitle}'.";
    }
}
