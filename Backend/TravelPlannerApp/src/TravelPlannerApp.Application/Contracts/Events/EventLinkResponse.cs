namespace TravelPlannerApp.Application.Contracts.Events;

public sealed record EventLinkResponse(
    string Id,
    string Description,
    string Url);
