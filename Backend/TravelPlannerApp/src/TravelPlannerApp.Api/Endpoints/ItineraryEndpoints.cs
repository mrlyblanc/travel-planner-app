using TravelPlannerApp.Api.Extensions;
using TravelPlannerApp.Application.Contracts.Itineraries;
using TravelPlannerApp.Application.Services;

namespace TravelPlannerApp.Api.Endpoints;

public static class ItineraryEndpoints
{
    public static IEndpointRouteBuilder MapItineraryEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/itineraries")
            .WithTags("Itineraries")
            .RequireCurrentUser();

        group.MapGet("/", async (IItineraryService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetAccessibleItinerariesAsync(cancellationToken)))
            .WithSummary("List accessible itineraries");

        group.MapGet("/{itineraryId}", async (string itineraryId, IItineraryService service, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            var response = await service.GetItineraryByIdAsync(itineraryId, cancellationToken);
            httpContext.Response.SetETag(response.Version);
            return TypedResults.Ok(response);
        })
            .WithSummary("Get itinerary");

        group.MapPost("/", async (CreateItineraryRequest request, IItineraryService service, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            var response = await service.CreateItineraryAsync(request, cancellationToken);
            httpContext.Response.SetETag(response.Version);
            return TypedResults.Created($"/api/itineraries/{response.Id}", response);
        })
            .Validate<CreateItineraryRequest>()
            .WithSummary("Create itinerary");

        group.MapPut("/{itineraryId}", async (string itineraryId, UpdateItineraryRequest request, IItineraryService service, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            var response = await service.UpdateItineraryAsync(itineraryId, httpContext.Request.GetIfMatchVersion(), request, cancellationToken);
            httpContext.Response.SetETag(response.Version);
            return TypedResults.Ok(response);
        })
            .Validate<UpdateItineraryRequest>()
            .RequireIfMatchHeader()
            .WithSummary("Update itinerary");

        return builder;
    }
}
