namespace TravelPlannerApp.Domain.Entities;

public sealed class EventLink
{
    public string Id { get; set; } = string.Empty;
    public string EventId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public Event? Event { get; set; }
}
