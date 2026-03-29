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

        group.MapGet("/me", async (IAuthService service, CancellationToken cancellationToken) =>
            TypedResults.Ok(await service.GetCurrentUserAsync(cancellationToken)))
            .RequireAuthorization(AuthorizationPolicies.AuthenticatedUser)
            .WithSummary("Get the authenticated user");

        return builder;
    }
}
