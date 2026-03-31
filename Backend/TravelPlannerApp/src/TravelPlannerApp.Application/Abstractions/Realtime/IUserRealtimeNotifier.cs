using TravelPlannerApp.Application.Contracts.Notifications;

namespace TravelPlannerApp.Application.Abstractions.Realtime;

public interface IUserRealtimeNotifier
{
    Task NotifyUsersAsync(IEnumerable<string> userIds, UserNotificationResponse notification, CancellationToken cancellationToken = default);
}
