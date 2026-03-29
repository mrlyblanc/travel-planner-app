using Microsoft.EntityFrameworkCore;
using TravelPlannerApp.Application.Abstractions.Persistence;
using TravelPlannerApp.Domain.Entities;

namespace TravelPlannerApp.Infrastructure.Persistence.Repositories;

public sealed class EventRepository : IEventRepository
{
    private readonly TravelPlannerDbContext _dbContext;

    public EventRepository(TravelPlannerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<Event>> ListByItineraryAsync(string itineraryId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Events
            .AsNoTracking()
            .Where(eventEntity => eventEntity.ItineraryId == itineraryId)
            .ToListAsync(cancellationToken);
    }

    public Task<Event?> GetByIdAsync(string eventId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Events
            .FirstOrDefaultAsync(eventEntity => eventEntity.Id == eventId, cancellationToken);
    }

    public Task AddAsync(Event eventEntity, CancellationToken cancellationToken = default)
    {
        return _dbContext.Events.AddAsync(eventEntity, cancellationToken).AsTask();
    }

    public void Remove(Event eventEntity)
    {
        _dbContext.Events.Remove(eventEntity);
    }

    public Task AddAuditLogAsync(EventAuditLog auditLog, CancellationToken cancellationToken = default)
    {
        return _dbContext.EventAuditLogs.AddAsync(auditLog, cancellationToken).AsTask();
    }

    public Task<List<EventAuditLog>> ListHistoryAsync(string eventId, CancellationToken cancellationToken = default)
    {
        return _dbContext.EventAuditLogs
            .AsNoTracking()
            .Where(log => log.EventId == eventId)
            .ToListAsync(cancellationToken);
    }

    public Task<string?> GetHistoryItineraryIdAsync(string eventId, CancellationToken cancellationToken = default)
    {
        return _dbContext.EventAuditLogs
            .AsNoTracking()
            .Where(log => log.EventId == eventId)
            .Select(log => log.ItineraryId)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
