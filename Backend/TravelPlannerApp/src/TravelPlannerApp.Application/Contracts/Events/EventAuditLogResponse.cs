using TravelPlannerApp.Domain.Enums;

namespace TravelPlannerApp.Application.Contracts.Events;

public sealed record EventAuditLogResponse(
    string Id,
    string EventId,
    string ItineraryId,
    EventAuditAction Action,
    string Summary,
    EventAuditSnapshotResponse Snapshot,
    string ChangedByUserId,
    DateTime ChangedAtUtc);
