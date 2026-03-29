using TravelPlannerApp.Domain.Enums;

namespace TravelPlannerApp.Application.Contracts.Events;

public sealed record EventResponse(
    string Id,
    string Version,
    string ItineraryId,
    string Title,
    string? Description,
    EventCategory Category,
    string? Color,
    DateTime StartDateTime,
    DateTime EndDateTime,
    string Timezone,
    string? Location,
    string? LocationAddress,
    decimal? LocationLat,
    decimal? LocationLng,
    decimal? Cost,
    string CreatedById,
    string UpdatedById,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
