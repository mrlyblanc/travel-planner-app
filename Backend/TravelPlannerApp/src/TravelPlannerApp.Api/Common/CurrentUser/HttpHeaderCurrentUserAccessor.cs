using TravelPlannerApp.Application.Abstractions.CurrentUser;

namespace TravelPlannerApp.Api.Common.CurrentUser;

public sealed class HttpHeaderCurrentUserAccessor : ICurrentUserAccessor
{
    private const string HeaderName = "X-User-Id";
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpHeaderCurrentUserAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? GetCurrentUserId()
    {
        return _httpContextAccessor.HttpContext?.Request.Headers[HeaderName].FirstOrDefault();
    }
}
