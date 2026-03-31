using TravelPlannerApp.Application.Contracts.Notifications;

namespace TravelPlannerApp.Application.Services;

public interface IUserNotificationService
{
    Task<IReadOnlyList<UserNotificationResponse>> GetCurrentUserNotificationsAsync(CancellationToken cancellationToken = default);
    Task<UserNotificationResponse> MarkAsReadAsync(string notificationId, CancellationToken cancellationToken = default);
    Task<int> MarkAllAsReadAsync(CancellationToken cancellationToken = default);
    Task DeleteAsync(string notificationId, CancellationToken cancellationToken = default);
}
