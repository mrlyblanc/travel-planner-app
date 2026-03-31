namespace TravelPlannerApp.Domain.Entities;

public sealed class Itinerary
{
    public string Id { get; set; } = string.Empty;
    public string ConcurrencyToken { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Destination { get; set; } = string.Empty;
    public string ShareCode { get; set; } = string.Empty;
    public DateTime ShareCodeUpdatedAtUtc { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string CreatedById { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public User? CreatedBy { get; set; }
    public ICollection<ItineraryMember> Members { get; set; } = new List<ItineraryMember>();
    public ICollection<Event> Events { get; set; } = new List<Event>();
}
