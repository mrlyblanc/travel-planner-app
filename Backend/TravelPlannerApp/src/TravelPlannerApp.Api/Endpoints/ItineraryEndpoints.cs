using Microsoft.AspNetCore.Authorization;
using TravelPlannerApp.Api.Common.Authorization;
using TravelPlannerApp.Api.Extensions;
using TravelPlannerApp.Application.Abstractions.Persistence;
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

        group.MapPut("/{itineraryId}", async Task<IResult> (string itineraryId, UpdateItineraryRequest request, IItineraryService service, IItineraryRepository itineraryRepository, IAuthorizationService authorizationService, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            var itinerary = await itineraryRepository.GetByIdAsync(itineraryId, cancellationToken);
            if (itinerary is not null)
            {
                var authorizationResult = await authorizationService.AuthorizeAsync(httpContext.User, itinerary, AuthorizationPolicies.ResourceOwner);
                if (!authorizationResult.Succeeded)
                {
                    return Results.Forbid();
                }
            }

            var response = await service.UpdateItineraryAsync(itineraryId, httpContext.Request.GetIfMatchVersion(), request, cancellationToken);
            httpContext.Response.SetETag(response.Version);
            return Results.Ok(response);
        })
            .Validate<UpdateItineraryRequest>()
            .RequireIfMatchHeader()
            .WithSummary("Update itinerary");

        group.MapGet("/{itineraryId}/share-code", async Task<IResult> (string itineraryId, IItineraryService service, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            var response = await service.GetShareCodeAsync(itineraryId, cancellationToken);
            httpContext.Response.SetETag(response.Version);
            return Results.Ok(response);
        })
            .WithSummary("Get itinerary share code");

        group.MapPost("/{itineraryId}/share-code/rotate", async Task<IResult> (string itineraryId, IItineraryService service, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            var response = await service.RotateShareCodeAsync(itineraryId, httpContext.Request.GetIfMatchVersion(), cancellationToken);
            httpContext.Response.SetETag(response.Version);
            return Results.Ok(response);
        })
            .RequireIfMatchHeader()
            .WithSummary("Rotate itinerary share code");

        group.MapPost("/join-by-code", async (JoinItineraryByCodeRequest request, IItineraryService service, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            var response = await service.JoinByCodeAsync(request, cancellationToken);
            httpContext.Response.SetETag(response.Version);
            return Results.Ok(response);
        })
            .Validate<JoinItineraryByCodeRequest>()
            .WithSummary("Join an itinerary with a share code");

        return builder;
    }
}
