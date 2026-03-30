using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.HttpsPolicy;

namespace TravelPlannerApp.Api.Extensions;

public static class TransportSecurityExtensions
{
    public static IServiceCollection AddTransportSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = 1;

            // Reverse-proxy hosts such as Azure App Service terminate TLS upstream.
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });

        services.AddHsts(options =>
        {
            options.MaxAge = TimeSpan.FromDays(GetPositiveInt(configuration, "TransportSecurity:Hsts:MaxAgeDays", 365));
            options.IncludeSubDomains = configuration.GetValue("TransportSecurity:Hsts:IncludeSubDomains", false);
            options.Preload = configuration.GetValue("TransportSecurity:Hsts:Preload", false);
            options.ExcludedHosts.Clear();
        });

        return services;
    }

    public static WebApplication UseTransportSecurity(this WebApplication app)
    {
        app.UseForwardedHeaders();

        if (!app.Environment.IsProduction())
        {
            app.UseHttpsRedirection();
            return app;
        }

        if (!app.Configuration.GetValue("TransportSecurity:EnforceHttpsInProduction", true))
        {
            return app;
        }

        app.UseHsts();
        app.Use(async (context, next) =>
        {
            if (!context.Request.IsHttps)
            {
                await Results.Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "HTTPS Required",
                        detail: "HTTPS is required for this API in production.")
                    .ExecuteAsync(context);
                return;
            }

            await next();
        });

        return app;
    }

    private static int GetPositiveInt(IConfiguration configuration, string key, int defaultValue)
    {
        var configuredValue = configuration.GetValue<int?>(key).GetValueOrDefault();
        return configuredValue > 0 ? configuredValue : defaultValue;
    }
}
