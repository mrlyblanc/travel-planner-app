using TravelPlannerApp.Domain.Enums;

namespace TravelPlannerApp.Application.Contracts.Events;

public sealed record EventAuditSnapshotResponse(
    string Id,
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
    string? CurrencyCode,
    string UpdatedById,
    DateTime UpdatedAtUtc);
