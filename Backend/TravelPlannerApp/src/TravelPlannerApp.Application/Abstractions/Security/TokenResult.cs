namespace TravelPlannerApp.Application.Abstractions.Security;

public sealed record TokenResult(string AccessToken, DateTime ExpiresAtUtc);
