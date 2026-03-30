using TravelPlannerApp.Api.Common.Authorization;
using TravelPlannerApp.Api.Common.RateLimiting;
using TravelPlannerApp.Api.Extensions;
using TravelPlannerApp.Application.Common.Exceptions;
using TravelPlannerApp.Application.Contracts.Auth;
using TravelPlannerApp.Application.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;

namespace TravelPlannerApp.Api.Endpoints;

public static class AuthEndpoints
{
    private const string RefreshTokenCookieName = "travelplanner.refresh";
    private const string RefreshTokenCookiePath = "/api/auth";

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/auth").WithTags("Auth");

        group.MapPost("/login", async (LoginRequest request, HttpContext httpContext, IAuthService service, CancellationToken cancellationToken) =>
        {
            var auth = await service.LoginAsync(request, cancellationToken);
            WriteRefreshTokenCookie(httpContext, auth.RefreshToken, auth.RefreshTokenExpiresAtUtc);
            return TypedResults.Ok(ToSessionResponse(auth));
        })
            .AllowAnonymous()
            .RequireRateLimiting(ApiRateLimitPolicyNames.AuthLogin)
            .Validate<LoginRequest>()
            .WithSummary("Login and get a JWT");

        group.MapPost("/refresh", async (RefreshTokenRequest? request, HttpContext httpContext, IAuthService service, CancellationToken cancellationToken) =>
        {
            var auth = await service.RefreshAsync(
                new RefreshTokenRequest
                {
                    RefreshToken = ResolveRefreshToken(httpContext, request)
                },
                cancellationToken);

            WriteRefreshTokenCookie(httpContext, auth.RefreshToken, auth.RefreshTokenExpiresAtUtc);
            return TypedResults.Ok(ToSessionResponse(auth));
        })
            .AllowAnonymous()
            .RequireRateLimiting(ApiRateLimitPolicyNames.AuthRefresh)
            .WithSummary("Refresh access and refresh tokens");

        group.MapPost("/logout", async (RefreshTokenRequest? request, HttpContext httpContext, IAuthService service, CancellationToken cancellationToken) =>
        {
            var refreshToken = TryResolveRefreshToken(httpContext, request);
            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                await service.LogoutAsync(
                    new RefreshTokenRequest
                    {
                        RefreshToken = refreshToken
                    },
                    cancellationToken);
            }

            DeleteRefreshTokenCookie(httpContext);
            return TypedResults.NoContent();
        })
            .AllowAnonymous()
            .RequireRateLimiting(ApiRateLimitPolicyNames.AuthMutation)
            .WithSummary("Revoke a refresh token");

        group.MapPost("/change-password", async (ChangePasswordRequest request, IAuthService service, CancellationToken cancellationToken) =>
        {
            await service.ChangePasswordAsync(request, cancellationToken);
            return TypedResults.NoContent();
        })
            .RequireAuthorization(AuthorizationPolicies.AuthenticatedUser)
            .RequireRateLimiting(ApiRateLimitPolicyNames.AuthMutation)
            .Validate<ChangePasswordRequest>()
            .WithSummary("Change the authenticated user's password");

        group.MapGet("/me", async (IAuthService service, CancellationToken cancellationToken) =>
            TypedResults.Ok(await service.GetCurrentUserAsync(cancellationToken)))
            .RequireAuthorization(AuthorizationPolicies.AuthenticatedUser)
            .WithSummary("Get the authenticated user");

        return builder;
    }

    private static AuthSessionResponse ToSessionResponse(AuthResponse auth)
    {
        return new AuthSessionResponse(
            auth.AccessToken,
            auth.TokenType,
            auth.ExpiresAtUtc,
            auth.RefreshTokenExpiresAtUtc,
            auth.User);
    }

    private static string ResolveRefreshToken(HttpContext httpContext, RefreshTokenRequest? request)
    {
        var refreshToken = TryResolveRefreshToken(httpContext, request);
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new UnauthorizedException("Refresh token is missing or expired.");
        }

        return refreshToken;
    }

    private static string? TryResolveRefreshToken(HttpContext httpContext, RefreshTokenRequest? request)
    {
        var requestToken = request?.RefreshToken?.Trim();
        if (!string.IsNullOrWhiteSpace(requestToken))
        {
            return requestToken;
        }

        return httpContext.Request.Cookies.TryGetValue(RefreshTokenCookieName, out var cookieToken)
            ? cookieToken?.Trim()
            : null;
    }

    private static void WriteRefreshTokenCookie(HttpContext httpContext, string refreshToken, DateTime expiresAtUtc)
    {
        httpContext.Response.Cookies.Append(
            RefreshTokenCookieName,
            refreshToken,
            CreateRefreshTokenCookieOptions(httpContext, expiresAtUtc));
    }

    private static void DeleteRefreshTokenCookie(HttpContext httpContext)
    {
        httpContext.Response.Cookies.Delete(
            RefreshTokenCookieName,
            CreateRefreshTokenCookieOptions(httpContext, DateTime.UtcNow.AddDays(-1)));
    }

    private static CookieOptions CreateRefreshTokenCookieOptions(HttpContext httpContext, DateTime expiresAtUtc)
    {
        var secure = ShouldUseSecureCookie(httpContext);

        return new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            Path = RefreshTokenCookiePath,
            Expires = new DateTimeOffset(DateTime.SpecifyKind(expiresAtUtc, DateTimeKind.Utc)),
            SameSite = secure && !IsLocalDevelopmentHost(httpContext) ? SameSiteMode.None : SameSiteMode.Lax,
            Secure = secure
        };
    }

    private static bool ShouldUseSecureCookie(HttpContext httpContext)
    {
        if (httpContext.Request.IsHttps)
        {
            return true;
        }

        return !IsLocalDevelopmentHost(httpContext);
    }

    private static bool IsLocalDevelopmentHost(HttpContext httpContext)
    {
        var host = httpContext.Request.Host.Host;
        return string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
            || string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase);
    }
}
