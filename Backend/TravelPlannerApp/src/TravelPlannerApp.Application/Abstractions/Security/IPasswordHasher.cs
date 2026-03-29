namespace TravelPlannerApp.Application.Abstractions.Security;

public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyHashedPassword(string hashedPassword, string providedPassword);
}
