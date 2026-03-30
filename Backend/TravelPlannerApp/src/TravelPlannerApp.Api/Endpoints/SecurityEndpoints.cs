namespace TravelPlannerApp.Api.Endpoints;

public static class SecurityEndpoints
{
    public static IEndpointRouteBuilder MapSecurityEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapPost("/security/csp-reports", async (HttpRequest request, ILoggerFactory loggerFactory) =>
        {
            using var reader = new StreamReader(request.Body);
            var payload = await reader.ReadToEndAsync();
            if (!string.IsNullOrWhiteSpace(payload))
            {
                loggerFactory.CreateLogger("TravelPlannerApp.Api.CspReports")
                    .LogWarning("Received CSP violation report: {Report}", payload);
            }

            return TypedResults.NoContent();
        })
            .AllowAnonymous()
            .WithTags("Security")
            .WithSummary("Accept Content Security Policy violation reports");

        return builder;
    }
}
