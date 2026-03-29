using TravelPlannerApp.Domain.Entities;

namespace TravelPlannerApp.Application.Abstractions.Persistence;

public interface IUserRepository
{
    Task<List<User>> ListAsync(CancellationToken cancellationToken = default);
    Task<List<User>> ListByIdsAsync(IEnumerable<string> userIds, CancellationToken cancellationToken = default);
    Task<User?> GetByIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
}
