using TravelPlannerApp.Api.Common.Errors;
using TravelPlannerApp.Api.Endpoints;
using TravelPlannerApp.Api.Realtime;

namespace TravelPlannerApp.Api.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseApiDefaults(this WebApplication app)
    {
        app.UseMiddleware<AppExceptionMiddleware>();
        app.UseSwagger();
        app.UseSwaggerUI();
        app.UseCors(ServiceCollectionExtensions.CorsPolicyName);
        app.UseHttpsRedirection();
        return app;
    }

    public static WebApplication MapApi(this WebApplication app)
    {
        app.MapGet("/", static () => Results.Redirect("/swagger"));

        var api = app.MapGroup("/api");
        api.MapUserEndpoints();
        api.MapItineraryEndpoints();
        api.MapItineraryMemberEndpoints();
        api.MapEventEndpoints();
        api.MapEventHistoryEndpoints();

        app.MapHub<ItineraryHub>("/hubs/itinerary");

        return app;
    }
}
