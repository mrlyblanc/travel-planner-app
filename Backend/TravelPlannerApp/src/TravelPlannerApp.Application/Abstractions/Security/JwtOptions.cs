namespace TravelPlannerApp.Application.Abstractions.Security;

public sealed class JwtOptions
{
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public int TokenLifetimeMinutes { get; set; } = 120;
    public int RefreshTokenLifetimeDays { get; set; } = 14;
}
