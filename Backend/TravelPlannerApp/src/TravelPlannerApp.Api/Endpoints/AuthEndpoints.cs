using TravelPlannerApp.Api.Common.Authorization;
using TravelPlannerApp.Api.Extensions;
using TravelPlannerApp.Application.Contracts.Auth;
using TravelPlannerApp.Application.Services;

namespace TravelPlannerApp.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/auth").WithTags("Auth");

        group.MapPost("/login", async (LoginRequest request, IAuthService service, CancellationToken cancellationToken) =>
            TypedResults.Ok(await service.LoginAsync(request, cancellationToken)))
            .AllowAnonymous()
            .Validate<LoginRequest>()
            .WithSummary("Login and get a JWT");

        group.MapPost("/refresh", async (RefreshTokenRequest request, IAuthService service, CancellationToken cancellationToken) =>
            TypedResults.Ok(await service.RefreshAsync(request, cancellationToken)))
            .AllowAnonymous()
            .Validate<RefreshTokenRequest>()
            .WithSummary("Refresh access and refresh tokens");

        group.MapPost("/logout", async (RefreshTokenRequest request, IAuthService service, CancellationToken cancellationToken) =>
        {
            await service.LogoutAsync(request, cancellationToken);
            return TypedResults.NoContent();
        })
            .AllowAnonymous()
            .Validate<RefreshTokenRequest>()
            .WithSummary("Revoke a refresh token");

        group.MapPost("/change-password", async (ChangePasswordRequest request, IAuthService service, CancellationToken cancellationToken) =>
        {
            await service.ChangePasswordAsync(request, cancellationToken);
            return TypedResults.NoContent();
        })
            .RequireAuthorization(AuthorizationPolicies.AuthenticatedUser)
            .Validate<ChangePasswordRequest>()
            .WithSummary("Change the authenticated user's password");

        group.MapGet("/me", async (IAuthService service, CancellationToken cancellationToken) =>
            TypedResults.Ok(await service.GetCurrentUserAsync(cancellationToken)))
            .RequireAuthorization(AuthorizationPolicies.AuthenticatedUser)
            .WithSummary("Get the authenticated user");

        return builder;
    }
}
