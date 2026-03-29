using TravelPlannerApp.Api.Common.Concurrency;
using TravelPlannerApp.Api.Common.Authorization;
using TravelPlannerApp.Api.Common.Swagger;
using TravelPlannerApp.Api.Common.Validation;
using TravelPlannerApp.Api.Common.Versioning;

namespace TravelPlannerApp.Api.Extensions;

public static class RouteBuilderExtensions
{
    public static RouteGroupBuilder RequireCurrentUser(this RouteGroupBuilder builder)
    {
        builder.RequireAuthorization(AuthorizationPolicies.AuthenticatedUser);
        return builder;
    }

    public static RouteGroupBuilder RequireApiVersionHeader(this RouteGroupBuilder builder)
    {
        builder.WithMetadata(new RequireApiVersionHeaderAttribute());
        return builder;
    }

    public static RouteHandlerBuilder Validate<TRequest>(this RouteHandlerBuilder builder) where TRequest : class
    {
        builder.AddEndpointFilter(new ValidationFilter<TRequest>());
        return builder;
    }

    public static RouteHandlerBuilder RequireIfMatchHeader(this RouteHandlerBuilder builder)
    {
        builder.WithMetadata(new RequireIfMatchHeaderAttribute());
        return builder;
    }
}
