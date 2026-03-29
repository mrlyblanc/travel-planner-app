using TravelPlannerApp.Domain.Entities;

namespace TravelPlannerApp.Application.Abstractions.Persistence;

public interface IItineraryRepository
{
    Task<List<Itinerary>> ListAccessibleAsync(string userId, CancellationToken cancellationToken = default);
    Task<Itinerary?> GetByIdAsync(string itineraryId, CancellationToken cancellationToken = default);
    Task<Itinerary?> GetAccessibleByIdAsync(string userId, string itineraryId, CancellationToken cancellationToken = default);
    Task<bool> IsMemberAsync(string itineraryId, string userId, CancellationToken cancellationToken = default);
    Task<List<ItineraryMember>> ListMembersAsync(string itineraryId, CancellationToken cancellationToken = default);
    Task AddAsync(Itinerary itinerary, CancellationToken cancellationToken = default);
    Task AddMembersAsync(IEnumerable<ItineraryMember> members, CancellationToken cancellationToken = default);
    void RemoveMembers(IEnumerable<ItineraryMember> members);
}
