using Microsoft.AspNetCore.Authorization;
using TravelPlannerApp.Api.Common.Authorization;
using TravelPlannerApp.Api.Extensions;
using TravelPlannerApp.Application.Abstractions.Persistence;
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

        group.MapPut("/", async Task<IResult> (string itineraryId, ReplaceItineraryMembersRequest request, IItineraryService service, IItineraryRepository itineraryRepository, IAuthorizationService authorizationService, HttpContext httpContext, CancellationToken cancellationToken) =>
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

            var members = await service.ReplaceMembersAsync(itineraryId, httpContext.Request.GetIfMatchVersion(), request, cancellationToken);
            var responseItinerary = await service.GetItineraryByIdAsync(itineraryId, cancellationToken);
            httpContext.Response.SetETag(responseItinerary.Version);
            return Results.Ok(members);
        })
            .Validate<ReplaceItineraryMembersRequest>()
            .RequireIfMatchHeader()
            .WithSummary("Replace itinerary members");

        group.MapDelete("/{userId}", async Task<IResult> (string itineraryId, string userId, IItineraryService service, IItineraryRepository itineraryRepository, IAuthorizationService authorizationService, HttpContext httpContext, CancellationToken cancellationToken) =>
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

            var members = await service.RemoveMemberAsync(itineraryId, userId, httpContext.Request.GetIfMatchVersion(), cancellationToken);
            var responseItinerary = await service.GetItineraryByIdAsync(itineraryId, cancellationToken);
            httpContext.Response.SetETag(responseItinerary.Version);
            return Results.Ok(members);
        })
            .RequireIfMatchHeader()
            .WithSummary("Remove a contributor from an itinerary");

        return builder;
    }
}
