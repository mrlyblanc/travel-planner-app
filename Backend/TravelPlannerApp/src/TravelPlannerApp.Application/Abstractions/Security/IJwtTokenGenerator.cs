using TravelPlannerApp.Domain.Entities;

namespace TravelPlannerApp.Application.Abstractions.Security;

public interface IJwtTokenGenerator
{
    TokenResult GenerateToken(User user);
}
