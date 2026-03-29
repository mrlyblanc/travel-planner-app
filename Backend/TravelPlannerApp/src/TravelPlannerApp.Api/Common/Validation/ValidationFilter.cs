using System.ComponentModel.DataAnnotations;

namespace TravelPlannerApp.Api.Common.Validation;

public sealed class ValidationFilter<TRequest> : IEndpointFilter where TRequest : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var request = context.Arguments.OfType<TRequest>().FirstOrDefault();
        if (request is null)
        {
            return await next(context);
        }

        var validationContext = new ValidationContext(request);
        var validationResults = new List<ValidationResult>();
        if (Validator.TryValidateObject(request, validationContext, validationResults, validateAllProperties: true))
        {
            return await next(context);
        }

        var errors = validationResults
            .SelectMany(result =>
            {
                var members = result.MemberNames.Any() ? result.MemberNames : [string.Empty];
                return members.Select(member => new KeyValuePair<string, string[]>(member, [result.ErrorMessage ?? "Validation failed."]));
            })
            .GroupBy(static pair => pair.Key)
            .ToDictionary(
                static group => group.Key,
                static group => group.SelectMany(pair => pair.Value).Distinct().ToArray());

        return Results.ValidationProblem(errors);
    }
}
