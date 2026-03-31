using TravelPlannerApp.Domain.Entities;

namespace TravelPlannerApp.Application.Abstractions.Persistence;

public interface IUserNotificationRepository
{
    Task<List<UserNotification>> ListByUserIdAsync(string userId, int limit = 50, CancellationToken cancellationToken = default);
    Task<UserNotification?> GetByIdAsync(string notificationId, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<UserNotification> notifications, CancellationToken cancellationToken = default);
    void Remove(UserNotification notification);
}
