using TravelPlannerApp.Api.Extensions;
using TravelPlannerApp.Application.Contracts.Events;
using TravelPlannerApp.Application.Services;

namespace TravelPlannerApp.Api.Endpoints;

public static class EventEndpoints
{
    public static IEndpointRouteBuilder MapEventEndpoints(this IEndpointRouteBuilder builder)
    {
        var itineraryGroup = builder.MapGroup("/itineraries/{itineraryId}/events")
            .WithTags("Events")
            .RequireCurrentUser();

        itineraryGroup.MapGet("/", async (string itineraryId, IEventService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetEventsAsync(itineraryId, cancellationToken)))
            .WithSummary("List itinerary events");

        itineraryGroup.MapPost("/", async (string itineraryId, CreateEventRequest request, IEventService service, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            var response = await service.CreateEventAsync(itineraryId, request, cancellationToken);
            httpContext.Response.SetETag(response.Version);
            return TypedResults.Created($"/api/events/{response.Id}", response);
        })
            .Validate<CreateEventRequest>()
            .WithSummary("Create event");

        var eventGroup = builder.MapGroup("/events")
            .WithTags("Events")
            .RequireCurrentUser();

        eventGroup.MapGet("/{eventId}", async (string eventId, IEventService service, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            var response = await service.GetEventByIdAsync(eventId, cancellationToken);
            httpContext.Response.SetETag(response.Version);
            return TypedResults.Ok(response);
        })
            .WithSummary("Get event");

        eventGroup.MapPut("/{eventId}", async (string eventId, UpdateEventRequest request, IEventService service, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            var response = await service.UpdateEventAsync(eventId, httpContext.Request.GetIfMatchVersion(), request, cancellationToken);
            httpContext.Response.SetETag(response.Version);
            return TypedResults.Ok(response);
        })
            .Validate<UpdateEventRequest>()
            .RequireIfMatchHeader()
            .WithSummary("Update event");

        eventGroup.MapDelete("/{eventId}", async (string eventId, IEventService service, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            await service.DeleteEventAsync(eventId, httpContext.Request.GetIfMatchVersion(), cancellationToken);
            return Results.NoContent();
        })
            .RequireIfMatchHeader()
            .WithSummary("Delete event");

        return builder;
    }
}
