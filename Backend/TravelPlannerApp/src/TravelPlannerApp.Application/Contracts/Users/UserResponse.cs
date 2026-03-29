namespace TravelPlannerApp.Application.Contracts.Users;

public sealed record UserResponse(
    string Id,
    string Version,
    string Name,
    string Email,
    string Avatar,
    DateTime CreatedAtUtc);
