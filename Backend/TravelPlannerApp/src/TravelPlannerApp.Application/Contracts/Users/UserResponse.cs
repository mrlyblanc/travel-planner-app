namespace TravelPlannerApp.Application.Contracts.Users;

public sealed record UserResponse(
    string Id,
    string Name,
    string Email,
    string Avatar,
    DateTime CreatedAtUtc);
