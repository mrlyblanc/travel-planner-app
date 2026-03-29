using Microsoft.AspNetCore.Authorization;
using TravelPlannerApp.Api.Common.Security;

namespace TravelPlannerApp.Api.Common.Authorization;

public sealed class StringResourceOwnerHandler : AuthorizationHandler<ResourceOwnerRequirement, string>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ResourceOwnerRequirement requirement, string resource)
    {
        var currentUserId = context.User.GetCurrentUserId();
        if (!string.IsNullOrWhiteSpace(currentUserId)
            && string.Equals(currentUserId, resource, StringComparison.OrdinalIgnoreCase))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
