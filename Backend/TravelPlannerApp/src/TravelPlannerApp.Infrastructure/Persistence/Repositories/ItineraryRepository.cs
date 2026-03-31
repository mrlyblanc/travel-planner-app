using Microsoft.EntityFrameworkCore;
using TravelPlannerApp.Application.Abstractions.Persistence;
using TravelPlannerApp.Domain.Entities;

namespace TravelPlannerApp.Infrastructure.Persistence.Repositories;

public sealed class ItineraryRepository : IItineraryRepository
{
    private readonly TravelPlannerDbContext _dbContext;

    public ItineraryRepository(TravelPlannerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<Itinerary>> ListAccessibleAsync(string userId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Itineraries
            .AsNoTracking()
            .Include(itinerary => itinerary.Members)
            .Where(itinerary => itinerary.Members.Any(member => member.UserId == userId))
            .ToListAsync(cancellationToken);
    }

    public Task<Itinerary?> GetByIdAsync(string itineraryId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Itineraries
            .Include(itinerary => itinerary.Members)
            .ThenInclude(member => member.User)
            .FirstOrDefaultAsync(itinerary => itinerary.Id == itineraryId, cancellationToken);
    }

    public Task<Itinerary?> GetAccessibleByIdAsync(string userId, string itineraryId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Itineraries
            .Include(itinerary => itinerary.Members)
            .ThenInclude(member => member.User)
            .FirstOrDefaultAsync(
                itinerary => itinerary.Id == itineraryId && itinerary.Members.Any(member => member.UserId == userId),
                cancellationToken);
    }

    public Task<Itinerary?> GetByShareCodeAsync(string shareCode, CancellationToken cancellationToken = default)
    {
        return _dbContext.Itineraries
            .Include(itinerary => itinerary.Members)
            .ThenInclude(member => member.User)
            .FirstOrDefaultAsync(itinerary => itinerary.ShareCode == shareCode, cancellationToken);
    }

    public Task<bool> ShareCodeExistsAsync(string shareCode, string? excludeItineraryId = null, CancellationToken cancellationToken = default)
    {
        return _dbContext.Itineraries.AnyAsync(
            itinerary => itinerary.ShareCode == shareCode && (excludeItineraryId == null || itinerary.Id != excludeItineraryId),
            cancellationToken);
    }

    public Task<bool> IsMemberAsync(string itineraryId, string userId, CancellationToken cancellationToken = default)
    {
        return _dbContext.ItineraryMembers
            .AnyAsync(member => member.ItineraryId == itineraryId && member.UserId == userId, cancellationToken);
    }

    public Task<List<ItineraryMember>> ListMembersAsync(string itineraryId, CancellationToken cancellationToken = default)
    {
        return _dbContext.ItineraryMembers
            .AsNoTracking()
            .Include(member => member.User)
            .Where(member => member.ItineraryId == itineraryId)
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(Itinerary itinerary, CancellationToken cancellationToken = default)
    {
        return _dbContext.Itineraries.AddAsync(itinerary, cancellationToken).AsTask();
    }

    public Task AddMembersAsync(IEnumerable<ItineraryMember> members, CancellationToken cancellationToken = default)
    {
        return _dbContext.ItineraryMembers.AddRangeAsync(members, cancellationToken);
    }

    public void RemoveMembers(IEnumerable<ItineraryMember> members)
    {
        _dbContext.ItineraryMembers.RemoveRange(members);
    }
}
