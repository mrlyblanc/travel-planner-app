using TravelPlannerApp.Domain.Entities;

namespace TravelPlannerApp.Application.Abstractions.Persistence;

public interface IPasswordResetTokenRepository
{
    Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<List<PasswordResetToken>> ListByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task AddAsync(PasswordResetToken token, CancellationToken cancellationToken = default);
}
