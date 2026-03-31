namespace TravelPlannerApp.Domain.Entities;

public sealed class UserNotification
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ItineraryId { get; set; }
    public string? ActorUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ReadAtUtc { get; set; }

    public User? User { get; set; }
    public Itinerary? Itinerary { get; set; }
    public User? ActorUser { get; set; }
}
