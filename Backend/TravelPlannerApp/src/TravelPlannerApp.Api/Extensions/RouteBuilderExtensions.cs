using TravelPlannerApp.Api.Common.Swagger;
using TravelPlannerApp.Api.Common.Validation;

namespace TravelPlannerApp.Api.Extensions;

public static class RouteBuilderExtensions
{
    public static RouteGroupBuilder RequireCurrentUser(this RouteGroupBuilder builder)
    {
        builder.WithMetadata(new RequireCurrentUserHeaderAttribute());
        return builder;
    }

    public static RouteHandlerBuilder Validate<TRequest>(this RouteHandlerBuilder builder) where TRequest : class
    {
        builder.AddEndpointFilter(new ValidationFilter<TRequest>());
        return builder;
    }
}
