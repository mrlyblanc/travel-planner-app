using TravelPlannerApp.Api.Extensions;
using TravelPlannerApp.Application.Services;

namespace TravelPlannerApp.Api.Endpoints;

public static class EventHistoryEndpoints
{
    public static IEndpointRouteBuilder MapEventHistoryEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/events/{eventId}/history")
            .WithTags("Event History")
            .RequireCurrentUser();

        group.MapGet("/", async (string eventId, IEventService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetHistoryAsync(eventId, cancellationToken)))
            .WithSummary("Get event history");

        return builder;
    }
}
