using Microsoft.EntityFrameworkCore;
using TravelPlannerApp.Application.Abstractions.Persistence;
using TravelPlannerApp.Domain.Entities;

namespace TravelPlannerApp.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly TravelPlannerDbContext _dbContext;

    public UserRepository(TravelPlannerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<User>> ListAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.Users
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public Task<List<User>> ListByIdsAsync(IEnumerable<string> userIds, CancellationToken cancellationToken = default)
    {
        var ids = userIds.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        return _dbContext.Users
            .Where(user => ids.Contains(user.Id))
            .ToListAsync(cancellationToken);
    }

    public Task<User?> GetByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Users.FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);
    }

    public Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
    {
        return _dbContext.Users.FirstOrDefaultAsync(user => user.Email == normalizedEmail, cancellationToken);
    }

    public Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        return _dbContext.Users.AddAsync(user, cancellationToken).AsTask();
    }
}
