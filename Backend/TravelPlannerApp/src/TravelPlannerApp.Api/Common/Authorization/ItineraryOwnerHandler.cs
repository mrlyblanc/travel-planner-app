using Microsoft.AspNetCore.Authorization;
using TravelPlannerApp.Api.Common.Security;
using TravelPlannerApp.Domain.Entities;

namespace TravelPlannerApp.Api.Common.Authorization;

public sealed class ItineraryOwnerHandler : AuthorizationHandler<ResourceOwnerRequirement, Itinerary>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ResourceOwnerRequirement requirement, Itinerary resource)
    {
        var currentUserId = context.User.GetCurrentUserId();
        if (!string.IsNullOrWhiteSpace(currentUserId)
            && string.Equals(currentUserId, resource.CreatedById, StringComparison.OrdinalIgnoreCase))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
