using TravelPlannerApp.Application.Contracts.Events;

namespace TravelPlannerApp.Application.Services;

public interface IEventService
{
    Task<IReadOnlyList<EventResponse>> GetEventsAsync(string itineraryId, CancellationToken cancellationToken = default);
    Task<EventResponse> GetEventByIdAsync(string eventId, CancellationToken cancellationToken = default);
    Task<EventResponse> CreateEventAsync(string itineraryId, CreateEventRequest request, CancellationToken cancellationToken = default);
    Task<EventResponse> UpdateEventAsync(string eventId, UpdateEventRequest request, CancellationToken cancellationToken = default);
    Task DeleteEventAsync(string eventId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EventAuditLogResponse>> GetHistoryAsync(string eventId, CancellationToken cancellationToken = default);
}
