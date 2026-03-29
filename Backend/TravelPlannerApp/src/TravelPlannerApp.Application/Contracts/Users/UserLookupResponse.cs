namespace TravelPlannerApp.Application.Contracts.Users;

public sealed record UserLookupResponse(
    string Id,
    string Name,
    string Email,
    string Avatar);
