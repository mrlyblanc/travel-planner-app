namespace TravelPlannerApp.Application.Abstractions.Security;

public interface IRefreshTokenGenerator
{
    string GenerateToken();
    string HashToken(string token);
}
