namespace TravelPlannerApp.Application.Common.Exceptions;

public sealed class UnauthorizedException : AppException
{
    public UnauthorizedException(string message) : base(message, 401)
    {
    }
}
