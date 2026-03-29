using TravelPlannerApp.Application.Contracts.Users;

namespace TravelPlannerApp.Application.Contracts.Auth;

public sealed record AuthResponse(
    string AccessToken,
    string RefreshToken,
    string TokenType,
    DateTime ExpiresAtUtc,
    DateTime RefreshTokenExpiresAtUtc,
    UserResponse User);
