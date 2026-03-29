using Asp.Versioning;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Net.Http.Headers;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using TravelPlannerApp.Api.Common.Authorization;
using TravelPlannerApp.Api.Common.CurrentUser;
using TravelPlannerApp.Api.Common.Swagger;
using TravelPlannerApp.Api.Common.Versioning;
using TravelPlannerApp.Api.Realtime;
using TravelPlannerApp.Application.Abstractions.CurrentUser;
using TravelPlannerApp.Application.Abstractions.Realtime;
using TravelPlannerApp.Application.Abstractions.Security;

namespace TravelPlannerApp.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public const string CorsPolicyName = "FrontendCors";

    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration, bool isDevelopment)
    {
        var jwtOptions = GetJwtOptions(configuration);

        services.ConfigureHttpJsonOptions(static options => ConfigureJson(options.SerializerOptions));
        services.AddSignalR().AddJsonProtocol(static options => ConfigureJson(options.PayloadSerializerOptions));
        services.AddSingleton(jwtOptions);
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.IncludeErrorDetails = isDevelopment;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        if (context.HttpContext.Request.Path.StartsWithSegments("/hubs/itinerary")
                            && context.Request.Query.TryGetValue("access_token", out var accessToken))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        if (isDevelopment)
                        {
                            var logger = CreateAuthenticationLogger(context.HttpContext);
                            var authorizationHeader = context.Request.Headers.Authorization.ToString();
                            var scheme = authorizationHeader.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "missing";
                            logger.LogWarning(
                                context.Exception,
                                "JWT authentication failed for {Method} {Path}. HasAuthorizationHeader={HasAuthorizationHeader}. AuthorizationScheme={AuthorizationScheme}.",
                                context.Request.Method,
                                context.Request.Path,
                                !string.IsNullOrWhiteSpace(authorizationHeader),
                                scheme);
                        }

                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        if (isDevelopment && (!string.IsNullOrWhiteSpace(context.Error) || context.Request.Headers.ContainsKey(HeaderNames.Authorization)))
                        {
                            var logger = CreateAuthenticationLogger(context.HttpContext);
                            logger.LogInformation(
                                "JWT challenge for {Method} {Path}. Error={Error}. Description={ErrorDescription}.",
                                context.Request.Method,
                                context.Request.Path,
                                context.Error ?? "none",
                                context.ErrorDescription ?? "none");
                        }

                        return Task.CompletedTask;
                    }
                };
            });
        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthorizationPolicies.AuthenticatedUser, policy => policy.RequireAuthenticatedUser());
            options.AddPolicy(AuthorizationPolicies.ResourceOwner, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.AddRequirements(new ResourceOwnerRequirement());
            });
        });
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
            var bearerScheme = new OpenApiSecurityScheme
            {
                Name = HeaderNames.Authorization,
                Type = SecuritySchemeType.Http,
                Scheme = JwtBearerDefaults.AuthenticationScheme,
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "JWT bearer token."
            };

            options.SwaggerDoc("v1", new() { Title = "TravelPlannerApp API", Version = "v1" });
            options.OperationFilter<ApiRequestHeadersOperationFilter>();
            options.AddSecurityDefinition("Bearer", bearerScheme);
            options.AddSecurityRequirement(static document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document, externalResource: null!)] = []
            });
            options.SupportNonNullableReferenceTypes();
        });

        services.AddHttpContextAccessor();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ICurrentUserAccessor, ClaimsCurrentUserAccessor>();
        services.AddSingleton<IAuthorizationHandler, StringResourceOwnerHandler>();
        services.AddSingleton<IAuthorizationHandler, ItineraryOwnerHandler>();
        services.AddSingleton<IAuthorizationHandler, EventOwnerHandler>();
        services.AddScoped<IItineraryRealtimeNotifier, SignalRItineraryRealtimeNotifier>();

        return services;
    }

    private static ILogger CreateAuthenticationLogger(HttpContext httpContext)
    {
        return httpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("TravelPlannerApp.Api.Authentication");
    }

    private static void ConfigureJson(JsonSerializerOptions options)
    {
        options.Converters.Add(new JsonStringEnumConverter());
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    }

    private static JwtOptions GetJwtOptions(IConfiguration configuration)
    {
        var jwtOptions = configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
        if (string.IsNullOrWhiteSpace(jwtOptions.Issuer)
            || string.IsNullOrWhiteSpace(jwtOptions.Audience)
            || string.IsNullOrWhiteSpace(jwtOptions.Secret)
            || jwtOptions.Secret.Length < 32)
        {
            throw new InvalidOperationException("Jwt settings are invalid. Configure Jwt:Issuer, Jwt:Audience, and a Jwt:Secret of at least 32 characters.");
        }

        if (jwtOptions.TokenLifetimeMinutes <= 0)
        {
            throw new InvalidOperationException("Jwt:TokenLifetimeMinutes must be greater than zero.");
        }

        return jwtOptions;
    }
}
