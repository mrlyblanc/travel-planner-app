using Microsoft.AspNetCore.Authorization;
using TravelPlannerApp.Api.Common.Authorization;
using TravelPlannerApp.Api.Extensions;
using TravelPlannerApp.Application.Contracts.Users;
using TravelPlannerApp.Application.Services;

namespace TravelPlannerApp.Api.Endpoints;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/users")
            .WithTags("Users")
            .RequireCurrentUser();

        group.MapGet("/", async (IUserService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetUsersAsync(cancellationToken)))
            .WithSummary("List users");

        group.MapGet("/{userId}", async (string userId, IUserService service, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            var response = await service.GetUserByIdAsync(userId, cancellationToken);
            httpContext.Response.SetETag(response.Version);
            return TypedResults.Ok(response);
        })
            .WithSummary("Get user");

        group.MapPost("/", async (CreateUserRequest request, IUserService service, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            var response = await service.CreateUserAsync(request, cancellationToken);
            httpContext.Response.SetETag(response.Version);
            return TypedResults.Created($"/api/users/{response.Id}", response);
        })
            .AllowAnonymous()
            .Validate<CreateUserRequest>()
            .WithSummary("Register a user");

        group.MapPut("/{userId}", async Task<IResult> (string userId, UpdateUserRequest request, IUserService service, IAuthorizationService authorizationService, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            var authorizationResult = await authorizationService.AuthorizeAsync(httpContext.User, userId, AuthorizationPolicies.ResourceOwner);
            if (!authorizationResult.Succeeded)
            {
                return Results.Forbid();
            }

            var response = await service.UpdateUserAsync(userId, httpContext.Request.GetIfMatchVersion(), request, cancellationToken);
            httpContext.Response.SetETag(response.Version);
            return Results.Ok(response);
        })
            .Validate<UpdateUserRequest>()
            .RequireIfMatchHeader()
            .WithSummary("Update user");

        return builder;
    }
}
