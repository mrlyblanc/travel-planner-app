namespace TravelPlannerApp.Application.Contracts.Itineraries;

public sealed record ItineraryResponse(
    string Id,
    string Version,
    string Title,
    string? Description,
    string Destination,
    DateOnly StartDate,
    DateOnly EndDate,
    string CreatedById,
    int MemberCount,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
