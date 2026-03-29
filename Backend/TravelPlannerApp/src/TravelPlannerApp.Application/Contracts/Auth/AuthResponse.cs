using TravelPlannerApp.Application.Contracts.Users;

namespace TravelPlannerApp.Application.Contracts.Auth;

public sealed record AuthResponse(
    string AccessToken,
    string TokenType,
    DateTime ExpiresAtUtc,
    UserResponse User);
