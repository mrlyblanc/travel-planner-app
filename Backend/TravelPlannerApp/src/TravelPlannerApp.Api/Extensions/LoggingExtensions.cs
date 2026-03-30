using Serilog;
using Serilog.Events;

namespace TravelPlannerApp.Api.Extensions;

public static class LoggingExtensions
{
    public static WebApplicationBuilder AddAppLogging(this WebApplicationBuilder builder)
    {
        var baseLoggerConfiguration = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.Console();

        if (builder.Environment.IsEnvironment("Testing"))
        {
            Log.Logger = baseLoggerConfiguration
                .MinimumLevel.Warning()
                .CreateLogger();
            builder.Host.UseSerilog(Log.Logger, dispose: true);
            return builder;
        }

        Log.Logger = baseLoggerConfiguration
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .CreateLogger();

        builder.Host.UseSerilog((context, services, loggerConfiguration) =>
        {
            loggerConfiguration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "TravelPlannerApp.Api");
        });

        return builder;
    }
}
