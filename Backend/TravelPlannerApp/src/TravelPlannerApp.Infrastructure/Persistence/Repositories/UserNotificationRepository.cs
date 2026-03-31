using Microsoft.EntityFrameworkCore;
using TravelPlannerApp.Application.Abstractions.Persistence;
using TravelPlannerApp.Domain.Entities;

namespace TravelPlannerApp.Infrastructure.Persistence.Repositories;

public sealed class UserNotificationRepository : IUserNotificationRepository
{
    private readonly TravelPlannerDbContext _dbContext;

    public UserNotificationRepository(TravelPlannerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<UserNotification>> ListByUserIdAsync(string userId, int limit = 50, CancellationToken cancellationToken = default)
    {
        return _dbContext.Set<UserNotification>()
            .AsNoTracking()
            .Where(notification => notification.UserId == userId)
            .OrderByDescending(notification => notification.CreatedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public Task<UserNotification?> GetByIdAsync(string notificationId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Set<UserNotification>()
            .FirstOrDefaultAsync(notification => notification.Id == notificationId, cancellationToken);
    }

    public Task AddRangeAsync(IEnumerable<UserNotification> notifications, CancellationToken cancellationToken = default)
    {
        return _dbContext.Set<UserNotification>().AddRangeAsync(notifications, cancellationToken);
    }

    public void Remove(UserNotification notification)
    {
        _dbContext.Set<UserNotification>().Remove(notification);
    }
}
