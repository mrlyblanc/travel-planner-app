using Microsoft.EntityFrameworkCore;
using TravelPlannerApp.Application.Abstractions.Persistence;
using TravelPlannerApp.Domain.Entities;

namespace TravelPlannerApp.Infrastructure.Persistence.Repositories;

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly TravelPlannerDbContext _dbContext;

    public RefreshTokenRepository(TravelPlannerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return _dbContext.RefreshTokens
            .Include(refreshToken => refreshToken.User)
            .FirstOrDefaultAsync(refreshToken => refreshToken.TokenHash == tokenHash, cancellationToken);
    }

    public Task<List<RefreshToken>> ListByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return _dbContext.RefreshTokens
            .Where(refreshToken => refreshToken.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        return _dbContext.RefreshTokens.AddAsync(refreshToken, cancellationToken).AsTask();
    }
}
