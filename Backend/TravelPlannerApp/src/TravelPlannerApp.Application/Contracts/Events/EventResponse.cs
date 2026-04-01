using TravelPlannerApp.Domain.Enums;

namespace TravelPlannerApp.Application.Contracts.Events;

public sealed record EventResponse(
    string Id,
    string Version,
    string ItineraryId,
    string Title,
    string? Description,
    string? Remarks,
    EventCategory Category,
    string? Color,
    bool IsAllDay,
    DateTime StartDateTime,
    DateTime EndDateTime,
    string Timezone,
    string? Location,
    string? LocationAddress,
    decimal? LocationLat,
    decimal? LocationLng,
    decimal? Cost,
    string? CurrencyCode,
    IReadOnlyList<EventLinkResponse> Links,
    string CreatedById,
    string UpdatedById,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
