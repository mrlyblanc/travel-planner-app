using TravelPlannerApp.Domain.Enums;

namespace TravelPlannerApp.Domain.Entities;

public sealed class Event
{
    public string Id { get; set; } = string.Empty;
    public string ItineraryId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public EventCategory Category { get; set; }
    public string? Color { get; set; }
    public DateTime StartDateTimeLocal { get; set; }
    public DateTime EndDateTimeLocal { get; set; }
    public string Timezone { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? LocationAddress { get; set; }
    public decimal? LocationLat { get; set; }
    public decimal? LocationLng { get; set; }
    public decimal? Cost { get; set; }
    public string CreatedById { get; set; } = string.Empty;
    public string UpdatedById { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public Itinerary? Itinerary { get; set; }
    public User? CreatedBy { get; set; }
    public User? UpdatedBy { get; set; }
    public ICollection<EventAuditLog> AuditLogs { get; set; } = new List<EventAuditLog>();
}
