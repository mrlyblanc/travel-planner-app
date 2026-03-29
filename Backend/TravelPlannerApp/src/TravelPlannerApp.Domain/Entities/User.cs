namespace TravelPlannerApp.Domain.Entities;

public sealed class User
{
    public string Id { get; set; } = string.Empty;
    public string ConcurrencyToken { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Avatar { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }

    public ICollection<Itinerary> CreatedItineraries { get; set; } = new List<Itinerary>();
    public ICollection<ItineraryMember> ItineraryMemberships { get; set; } = new List<ItineraryMember>();
    public ICollection<ItineraryMember> AddedItineraryMembers { get; set; } = new List<ItineraryMember>();
    public ICollection<Event> CreatedEvents { get; set; } = new List<Event>();
    public ICollection<Event> UpdatedEvents { get; set; } = new List<Event>();
    public ICollection<EventAuditLog> EventAuditLogs { get; set; } = new List<EventAuditLog>();
}
