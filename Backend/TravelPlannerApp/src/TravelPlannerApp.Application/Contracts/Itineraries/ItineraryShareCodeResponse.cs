namespace TravelPlannerApp.Application.Contracts.Itineraries;

public sealed record ItineraryShareCodeResponse(
    string ItineraryId,
    string Version,
    string Code,
    DateTime UpdatedAtUtc);
