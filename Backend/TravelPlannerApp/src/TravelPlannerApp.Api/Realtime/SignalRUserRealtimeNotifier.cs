using Microsoft.AspNetCore.SignalR;
using TravelPlannerApp.Application.Abstractions.Realtime;
using TravelPlannerApp.Application.Contracts.Notifications;

namespace TravelPlannerApp.Api.Realtime;

public sealed class SignalRUserRealtimeNotifier : IUserRealtimeNotifier
{
    private readonly IHubContext<ItineraryHub> _hubContext;

    public SignalRUserRealtimeNotifier(IHubContext<ItineraryHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task NotifyUsersAsync(IEnumerable<string> userIds, UserNotificationResponse notification, CancellationToken cancellationToken = default)
    {
        var groups = userIds
            .Where(static userId => !string.IsNullOrWhiteSpace(userId))
            .Select(ItineraryHub.UserGroupName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (groups.Length == 0)
        {
            return Task.CompletedTask;
        }

        return _hubContext.Clients
            .Groups(groups)
            .SendAsync("userNotification", notification, cancellationToken);
    }
}
