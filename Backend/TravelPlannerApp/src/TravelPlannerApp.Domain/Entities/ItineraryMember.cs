namespace TravelPlannerApp.Domain.Entities;

public sealed class ItineraryMember
{
    public string ItineraryId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string AddedByUserId { get; set; } = string.Empty;
    public DateTime AddedAtUtc { get; set; }

    public Itinerary? Itinerary { get; set; }
    public User? User { get; set; }
    public User? AddedByUser { get; set; }
}
