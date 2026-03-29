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

        group.MapGet("/", async (string itineraryId, IItineraryService service, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            var itinerary = await service.GetItineraryByIdAsync(itineraryId, cancellationToken);
            var members = await service.GetMembersAsync(itineraryId, cancellationToken);
            httpContext.Response.SetETag(itinerary.Version);
            return TypedResults.Ok(members);
        })
            .WithSummary("List itinerary members");

        group.MapPut("/", async (string itineraryId, ReplaceItineraryMembersRequest request, IItineraryService service, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            var members = await service.ReplaceMembersAsync(itineraryId, httpContext.Request.GetIfMatchVersion(), request, cancellationToken);
            var itinerary = await service.GetItineraryByIdAsync(itineraryId, cancellationToken);
            httpContext.Response.SetETag(itinerary.Version);
            return TypedResults.Ok(members);
        })
            .Validate<ReplaceItineraryMembersRequest>()
            .RequireIfMatchHeader()
            .WithSummary("Replace itinerary members");

        return builder;
    }
}
