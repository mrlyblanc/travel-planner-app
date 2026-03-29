using TravelPlannerApp.Domain.Enums;

namespace TravelPlannerApp.Domain.Entities;

public sealed class EventAuditLog
{
    public string Id { get; set; } = string.Empty;
    public string EventId { get; set; } = string.Empty;
    public string ItineraryId { get; set; } = string.Empty;
    public EventAuditAction Action { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string SnapshotJson { get; set; } = string.Empty;
    public string ChangedByUserId { get; set; } = string.Empty;
    public DateTime ChangedAtUtc { get; set; }

    public Event? Event { get; set; }
    public Itinerary? Itinerary { get; set; }
    public User? ChangedByUser { get; set; }
}
