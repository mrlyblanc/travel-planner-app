using TravelPlannerApp.Api.Common.Configuration;
using TravelPlannerApp.Api.Extensions;
using TravelPlannerApp.Application;
using TravelPlannerApp.Infrastructure;

DotEnvLoader.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment.IsDevelopment());
builder.Services.AddApiServices(builder.Configuration);

var app = builder.Build();

await app.Services.InitialiseDatabaseAsync();

app.UseApiDefaults();
app.MapApi();

app.Run();

public partial class Program;
