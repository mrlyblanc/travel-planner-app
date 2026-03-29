using TravelPlannerApp.Domain.Entities;

namespace TravelPlannerApp.Application.Abstractions.Persistence;

public interface IEventRepository
{
    Task<List<Event>> ListByItineraryAsync(string itineraryId, CancellationToken cancellationToken = default);
    Task<Event?> GetByIdAsync(string eventId, CancellationToken cancellationToken = default);
    Task AddAsync(Event eventEntity, CancellationToken cancellationToken = default);
    void Remove(Event eventEntity);
    Task AddAuditLogAsync(EventAuditLog auditLog, CancellationToken cancellationToken = default);
    Task<List<EventAuditLog>> ListHistoryAsync(string eventId, CancellationToken cancellationToken = default);
    Task<string?> GetHistoryItineraryIdAsync(string eventId, CancellationToken cancellationToken = default);
}
