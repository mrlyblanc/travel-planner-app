using Microsoft.EntityFrameworkCore;
using TravelPlannerApp.Application.Abstractions.Persistence;
using TravelPlannerApp.Domain.Entities;

namespace TravelPlannerApp.Infrastructure.Persistence.Repositories;

public sealed class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly TravelPlannerDbContext _dbContext;

    public PasswordResetTokenRepository(TravelPlannerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return _dbContext.PasswordResetTokens
            .Include(token => token.User)
            .FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);
    }

    public Task<List<PasswordResetToken>> ListByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return _dbContext.PasswordResetTokens
            .Where(token => token.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(PasswordResetToken token, CancellationToken cancellationToken = default)
    {
        return _dbContext.PasswordResetTokens.AddAsync(token, cancellationToken).AsTask();
    }
}
