namespace TravelPlannerApp.Application.Common.Exceptions;

public sealed class ForbiddenException : AppException
{
    public ForbiddenException(string message) : base(message, 403)
    {
    }
}
