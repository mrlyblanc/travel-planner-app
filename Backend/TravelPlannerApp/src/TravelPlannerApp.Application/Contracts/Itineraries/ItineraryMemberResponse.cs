namespace TravelPlannerApp.Application.Contracts.Itineraries;

public sealed record ItineraryMemberResponse(
    string ItineraryId,
    string UserId,
    string Name,
    string Email,
    string Avatar,
    string AddedByUserId,
    DateTime AddedAtUtc);
