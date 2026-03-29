using System.Security.Claims;

namespace TravelPlannerApp.Api.Common.Security;

public static class ClaimsPrincipalExtensions
{
    private static readonly string[] UserIdClaimTypes =
    [
        ClaimTypes.NameIdentifier,
        "sub",
        "nameid"
    ];

    public static string? GetCurrentUserId(this ClaimsPrincipal? principal)
    {
        if (principal is null)
        {
            return null;
        }

        foreach (var claimType in UserIdClaimTypes)
        {
            var value = principal.FindFirstValue(claimType);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }
}
