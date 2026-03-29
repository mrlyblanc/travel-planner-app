using Asp.Versioning;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Net.Http.Headers;
using TravelPlannerApp.Api.Common.CurrentUser;
using TravelPlannerApp.Api.Common.Swagger;
using TravelPlannerApp.Api.Common.Versioning;
using TravelPlannerApp.Api.Realtime;
using TravelPlannerApp.Application.Abstractions.CurrentUser;
using TravelPlannerApp.Application.Abstractions.Realtime;

namespace TravelPlannerApp.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public const string CorsPolicyName = "FrontendCors";

    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.ConfigureHttpJsonOptions(static options => ConfigureJson(options.SerializerOptions));
        services.AddSignalR().AddJsonProtocol(static options => ConfigureJson(options.PayloadSerializerOptions));
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ApiVersionReader = new HeaderApiVersionReader(ApiVersioningConstants.HeaderName);
            options.ReportApiVersions = true;
        });
        services.AddCors(options =>
        {
            var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? ["http://localhost:5173", "http://127.0.0.1:5173"];

            options.AddPolicy(CorsPolicyName, policy =>
            {
                policy.WithOrigins(origins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .WithExposedHeaders(HeaderNames.ETag, "api-supported-versions")
                    .AllowCredentials();
            });
        });

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new() { Title = "TravelPlannerApp API", Version = "v1" });
            options.OperationFilter<XUserIdHeaderOperationFilter>();
            options.SupportNonNullableReferenceTypes();
        });

        services.AddHttpContextAccessor();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ICurrentUserAccessor, HttpHeaderCurrentUserAccessor>();
        services.AddScoped<IItineraryRealtimeNotifier, SignalRItineraryRealtimeNotifier>();

        return services;
    }

    private static void ConfigureJson(JsonSerializerOptions options)
    {
        options.Converters.Add(new JsonStringEnumConverter());
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    }
}
