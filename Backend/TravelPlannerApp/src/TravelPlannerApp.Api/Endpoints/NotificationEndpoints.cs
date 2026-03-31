using TravelPlannerApp.Api.Extensions;
using TravelPlannerApp.Application.Services;

namespace TravelPlannerApp.Api.Endpoints;

public static class NotificationEndpoints
{
    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/notifications")
            .WithTags("Notifications")
            .RequireCurrentUser();

        group.MapGet("/", async (IUserNotificationService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetCurrentUserNotificationsAsync(cancellationToken)))
            .WithSummary("List notifications for the current user");

        group.MapPost("/{notificationId}/read", async (string notificationId, IUserNotificationService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.MarkAsReadAsync(notificationId, cancellationToken)))
            .WithSummary("Mark a notification as read");

        group.MapPost("/read-all", async (IUserNotificationService service, CancellationToken cancellationToken) =>
        {
            var markedCount = await service.MarkAllAsReadAsync(cancellationToken);
            return Results.Ok(new { markedCount });
        })
            .WithSummary("Mark all notifications as read");

        group.MapDelete("/{notificationId}", async (string notificationId, IUserNotificationService service, CancellationToken cancellationToken) =>
        {
            await service.DeleteAsync(notificationId, cancellationToken);
            return Results.NoContent();
        })
            .WithSummary("Delete a notification");

        return builder;
    }
}
