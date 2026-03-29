namespace TravelPlannerApp.Application.Abstractions.CurrentUser;

public interface ICurrentUserAccessor
{
    string? GetCurrentUserId();
}
