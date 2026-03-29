using TravelPlannerApp.Application.Contracts.Itineraries;

namespace TravelPlannerApp.Application.Services;

public interface IItineraryService
{
    Task<IReadOnlyList<ItineraryResponse>> GetAccessibleItinerariesAsync(CancellationToken cancellationToken = default);
    Task<ItineraryResponse> GetItineraryByIdAsync(string itineraryId, CancellationToken cancellationToken = default);
    Task<ItineraryResponse> CreateItineraryAsync(CreateItineraryRequest request, CancellationToken cancellationToken = default);
    Task<ItineraryResponse> UpdateItineraryAsync(string itineraryId, UpdateItineraryRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ItineraryMemberResponse>> GetMembersAsync(string itineraryId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ItineraryMemberResponse>> ReplaceMembersAsync(string itineraryId, ReplaceItineraryMembersRequest request, CancellationToken cancellationToken = default);
}
