using TravelPlannerApp.Application.Contracts.Users;

namespace TravelPlannerApp.Application.Contracts.Auth;

public sealed record AuthSessionResponse(
    string AccessToken,
    string TokenType,
    DateTime ExpiresAtUtc,
    DateTime RefreshTokenExpiresAtUtc,
    UserResponse User);
