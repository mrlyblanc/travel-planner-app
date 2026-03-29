using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using TravelPlannerApp.Api.Common.Concurrency;
using TravelPlannerApp.Api.Common.Versioning;

namespace TravelPlannerApp.Api.Common.Swagger;

public sealed class ApiRequestHeadersOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata ?? [];
        var requiresApiVersion = metadata.OfType<RequireApiVersionHeaderAttribute>().Any();
        var requiresIfMatch = metadata.OfType<RequireIfMatchHeaderAttribute>().Any();

        if (!requiresApiVersion && !requiresIfMatch)
        {
            return;
        }

        operation.Parameters ??= [];

        if (requiresApiVersion && operation.Parameters.All(static parameter => !string.Equals(parameter.Name, ApiVersioningConstants.HeaderName, StringComparison.OrdinalIgnoreCase)))
        {
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = ApiVersioningConstants.HeaderName,
                In = ParameterLocation.Header,
                Required = false,
                Description = "Requested API version. Defaults to 1.0 when omitted.",
                Schema = new OpenApiSchema
                {
                    Type = JsonSchemaType.String
                }
            });
        }

        if (!requiresIfMatch || operation.Parameters.Any(static parameter => string.Equals(parameter.Name, "If-Match", StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "If-Match",
            In = ParameterLocation.Header,
            Required = true,
            Description = "Current resource ETag required for optimistic concurrency.",
            Schema = new OpenApiSchema
            {
                Type = JsonSchemaType.String
            }
        });
    }
}
