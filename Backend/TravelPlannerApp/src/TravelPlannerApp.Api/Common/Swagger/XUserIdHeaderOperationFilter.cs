using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace TravelPlannerApp.Api.Common.Swagger;

public sealed class XUserIdHeaderOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var requiresCurrentUser = (context.ApiDescription.ActionDescriptor.EndpointMetadata ?? [])
            .OfType<RequireCurrentUserHeaderAttribute>()
            .Any();

        if (!requiresCurrentUser)
        {
            return;
        }

        operation.Parameters ??= [];
        if (operation.Parameters.Any(static parameter => string.Equals(parameter.Name, "X-User-Id", StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "X-User-Id",
            In = ParameterLocation.Header,
            Required = true,
            Description = "Current user id for local development.",
            Schema = new OpenApiSchema
            {
                Type = JsonSchemaType.String
            }
        });
    }
}
