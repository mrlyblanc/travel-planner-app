using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Net.Http.Headers;
using TravelPlannerApp.Api.Common.RateLimiting;
using TravelPlannerApp.Api.Common.Security;

namespace TravelPlannerApp.Api.Extensions;

public static class RateLimitingExtensions
{
    private const string ForwardedForHeaderName = "X-Forwarded-For";

    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds).ToString("0");
                }

                await Results.Problem(
                        statusCode: StatusCodes.Status429TooManyRequests,
                        title: "Too Many Requests",
                        detail: "Too many requests. Try again later.")
                    .ExecuteAsync(context.HttpContext);
            };

            options.AddPolicy(ApiRateLimitPolicyNames.AuthLogin, context =>
                CreateFixedWindowPartition(
                    context,
                    scope: "auth-login",
                    permitLimit: GetPositiveInt(configuration, "RateLimiting:Auth:Login:PermitLimit", 5),
                    window: TimeSpan.FromSeconds(GetPositiveInt(configuration, "RateLimiting:Auth:Login:WindowSeconds", 60)),
                    preferAuthenticatedUser: false));

            options.AddPolicy(ApiRateLimitPolicyNames.AuthRefresh, context =>
                CreateFixedWindowPartition(
                    context,
                    scope: "auth-refresh",
                    permitLimit: GetPositiveInt(configuration, "RateLimiting:Auth:Refresh:PermitLimit", 10),
                    window: TimeSpan.FromSeconds(GetPositiveInt(configuration, "RateLimiting:Auth:Refresh:WindowSeconds", 60)),
                    preferAuthenticatedUser: false));

            options.AddPolicy(ApiRateLimitPolicyNames.AuthMutation, context =>
                CreateFixedWindowPartition(
                    context,
                    scope: "auth-mutation",
                    permitLimit: GetPositiveInt(configuration, "RateLimiting:Auth:Mutation:PermitLimit", 10),
                    window: TimeSpan.FromSeconds(GetPositiveInt(configuration, "RateLimiting:Auth:Mutation:WindowSeconds", 300)),
                    preferAuthenticatedUser: true));

            options.AddPolicy(ApiRateLimitPolicyNames.ItineraryShareCodeRotate, context =>
                CreateFixedWindowPartition(
                    context,
                    scope: "itinerary-share-code-rotate",
                    permitLimit: GetPositiveInt(configuration, "RateLimiting:Itinerary:ShareCodeRotate:PermitLimit", 5),
                    window: TimeSpan.FromSeconds(GetPositiveInt(configuration, "RateLimiting:Itinerary:ShareCodeRotate:WindowSeconds", 60)),
                    preferAuthenticatedUser: true));
        });

        return services;
    }

    private static RateLimitPartition<string> CreateFixedWindowPartition(
        HttpContext httpContext,
        string scope,
        int permitLimit,
        TimeSpan window,
        bool preferAuthenticatedUser)
    {
        var partitionKey = GetPartitionKey(httpContext, scope, preferAuthenticatedUser);
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = window,
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                AutoReplenishment = true
            });
    }

    private static string GetPartitionKey(HttpContext httpContext, string scope, bool preferAuthenticatedUser)
    {
        var currentUserId = httpContext.User.GetCurrentUserId();
        if (preferAuthenticatedUser && !string.IsNullOrWhiteSpace(currentUserId))
        {
            return $"{scope}:user:{currentUserId.Trim().ToLowerInvariant()}";
        }

        var forwardedFor = httpContext.Request.Headers[ForwardedForHeaderName].ToString();
        var forwardedIp = forwardedFor
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();
        var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString();
        var clientKey = !string.IsNullOrWhiteSpace(forwardedIp)
            ? forwardedIp
            : !string.IsNullOrWhiteSpace(remoteIp)
                ? remoteIp
                : "unknown";

        return $"{scope}:client:{clientKey.ToLowerInvariant()}";
    }

    private static int GetPositiveInt(IConfiguration configuration, string key, int defaultValue)
    {
        var configuredValue = configuration.GetValue<int?>(key).GetValueOrDefault();
        return configuredValue > 0 ? configuredValue : defaultValue;
    }
}
