namespace TravelPlannerApp.Application.Common.Exceptions;

public sealed class PreconditionFailedException : AppException
{
    public PreconditionFailedException(string message) : base(message, 412)
    {
    }
}
