using TravelPlannerApp.Api.Extensions;
using TravelPlannerApp.Application.Contracts.Users;
using TravelPlannerApp.Application.Services;

namespace TravelPlannerApp.Api.Endpoints;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/users").WithTags("Users");

        group.MapGet("/", async (IUserService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetUsersAsync(cancellationToken)))
            .WithSummary("List users");

        group.MapGet("/{userId}", async (string userId, IUserService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetUserByIdAsync(userId, cancellationToken)))
            .WithSummary("Get user");

        group.MapPost("/", async (CreateUserRequest request, IUserService service, CancellationToken cancellationToken) =>
        {
            var response = await service.CreateUserAsync(request, cancellationToken);
            return Results.Created($"/api/users/{response.Id}", response);
        })
            .Validate<CreateUserRequest>()
            .WithSummary("Create user");

        group.MapPut("/{userId}", async (string userId, UpdateUserRequest request, IUserService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.UpdateUserAsync(userId, request, cancellationToken)))
            .Validate<UpdateUserRequest>()
            .WithSummary("Update user");

        return builder;
    }
}
