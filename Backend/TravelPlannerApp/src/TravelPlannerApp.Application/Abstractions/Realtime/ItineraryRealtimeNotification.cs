namespace TravelPlannerApp.Application.Abstractions.Realtime;

public sealed record ItineraryRealtimeNotification(
    string Type,
    string ItineraryId,
    string EntityId,
    DateTime OccurredAtUtc,
    object? Payload = null);
