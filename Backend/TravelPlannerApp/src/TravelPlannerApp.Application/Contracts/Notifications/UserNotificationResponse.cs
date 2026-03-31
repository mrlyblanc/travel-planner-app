namespace TravelPlannerApp.Application.Contracts.Notifications;

public sealed record UserNotificationResponse(
    string Id,
    string Type,
    string Title,
    string Message,
    string? ItineraryId,
    string? ActorUserId,
    DateTime CreatedAtUtc,
    DateTime? ReadAtUtc);
