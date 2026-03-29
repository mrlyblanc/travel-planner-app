using Serilog;
using TravelPlannerApp.Api.Common.Configuration;
using TravelPlannerApp.Api.Extensions;
using TravelPlannerApp.Application;
using TravelPlannerApp.Infrastructure;

DotEnvLoader.Load();

var builder = WebApplication.CreateBuilder(args);
builder.AddAppLogging();

try
{
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration, builder.Environment.IsDevelopment());
    builder.Services.AddApiServices(builder.Configuration, builder.Environment.IsDevelopment());

    var app = builder.Build();

    await app.Services.InitialiseDatabaseAsync();

    app.Logger.LogInformation("Starting TravelPlannerApp API with SQL Server");

    app.UseApiDefaults();
    app.MapApi();

    app.Run();
}
catch (Exception exception)
{
    Log.Fatal(exception, "TravelPlannerApp API terminated unexpectedly");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}

public partial class Program;
