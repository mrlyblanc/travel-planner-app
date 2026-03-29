using TravelPlannerApp.Api.Common.Security;
using TravelPlannerApp.Application.Abstractions.CurrentUser;

namespace TravelPlannerApp.Api.Common.CurrentUser;

public sealed class ClaimsCurrentUserAccessor : ICurrentUserAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ClaimsCurrentUserAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? GetCurrentUserId()
    {
        return _httpContextAccessor.HttpContext?.User.GetCurrentUserId();
    }
}
