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

        group.MapGet("/{itineraryId}", async (string itineraryId, IItineraryService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetItineraryByIdAsync(itineraryId, cancellationToken)))
            .WithSummary("Get itinerary");

        group.MapPost("/", async (CreateItineraryRequest request, IItineraryService service, CancellationToken cancellationToken) =>
        {
            var response = await service.CreateItineraryAsync(request, cancellationToken);
            return Results.Created($"/api/itineraries/{response.Id}", response);
        })
            .Validate<CreateItineraryRequest>()
            .WithSummary("Create itinerary");

        group.MapPut("/{itineraryId}", async (string itineraryId, UpdateItineraryRequest request, IItineraryService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.UpdateItineraryAsync(itineraryId, request, cancellationToken)))
            .Validate<UpdateItineraryRequest>()
            .WithSummary("Update itinerary");

        return builder;
    }
}
