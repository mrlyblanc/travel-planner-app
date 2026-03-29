using TravelPlannerApp.Api.Extensions;
using TravelPlannerApp.Application.Contracts.Itineraries;
using TravelPlannerApp.Application.Services;

namespace TravelPlannerApp.Api.Endpoints;

public static class ItineraryMemberEndpoints
{
    public static IEndpointRouteBuilder MapItineraryMemberEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/itineraries/{itineraryId}/members")
            .WithTags("Members")
            .RequireCurrentUser();

        group.MapGet("/", async (string itineraryId, IItineraryService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetMembersAsync(itineraryId, cancellationToken)))
            .WithSummary("List itinerary members");

        group.MapPut("/", async (string itineraryId, ReplaceItineraryMembersRequest request, IItineraryService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.ReplaceMembersAsync(itineraryId, request, cancellationToken)))
            .Validate<ReplaceItineraryMembersRequest>()
            .WithSummary("Replace itinerary members");

        return builder;
    }
}
