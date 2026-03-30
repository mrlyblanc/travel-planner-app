using Asp.Versioning;
using System.Security.Claims;
using Serilog;
using TravelPlannerApp.Api.Common.Authorization;
using TravelPlannerApp.Api.Common.Errors;
using TravelPlannerApp.Api.Common.Security;
using TravelPlannerApp.Api.Endpoints;
using TravelPlannerApp.Api.Realtime;

namespace TravelPlannerApp.Api.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseApiDefaults(this WebApplication app)
    {
        app.UseTransportSecurity();
        app.UseSerilogRequestLogging(options =>
        {
            options.GetLevel = static (httpContext, _, exception) =>
            {
                if (exception is not null || httpContext.Response.StatusCode >= 500)
                {
                    return Serilog.Events.LogEventLevel.Error;
                }

                if (httpContext.Response.StatusCode >= 400)
                {
                    return Serilog.Events.LogEventLevel.Warning;
                }

                return Serilog.Events.LogEventLevel.Information;
            };

            options.EnrichDiagnosticContext = static (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("TraceId", httpContext.TraceIdentifier);
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value ?? string.Empty);
                diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
                diagnosticContext.Set("CurrentUserId", httpContext.User.GetCurrentUserId() ?? "anonymous");
            };
        });
        app.UseMiddleware<AppExceptionMiddleware>();
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseCors(ServiceCollectionExtensions.CorsPolicyName);
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        return app;
    }

    public static WebApplication MapApi(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapGet("/", static () => Results.Redirect("/swagger"));
        }
        else
        {
            app.MapGet("/", static () => Results.Ok(new { name = "TravelPlannerApp API" }));
        }

        app.MapSecurityEndpoints();

        var apiVersionSet = app.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        var api = app.MapGroup("/api")
            .WithApiVersionSet(apiVersionSet)
            .HasApiVersion(new ApiVersion(1, 0))
            .RequireApiVersionHeader();

        api.MapAuthEndpoints();
        api.MapUserEndpoints();
        api.MapItineraryEndpoints();
        api.MapItineraryMemberEndpoints();
        api.MapEventEndpoints();
        api.MapEventHistoryEndpoints();

        app.MapHub<ItineraryHub>("/hubs/itinerary")
            .RequireAuthorization(AuthorizationPolicies.AuthenticatedUser);

        return app;
    }
}
