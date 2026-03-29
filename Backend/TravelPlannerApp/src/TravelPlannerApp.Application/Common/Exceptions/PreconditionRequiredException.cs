namespace TravelPlannerApp.Application.Common.Exceptions;

public sealed class PreconditionRequiredException : AppException
{
    public PreconditionRequiredException(string message) : base(message, 428)
    {
    }
}
