using System.ComponentModel.DataAnnotations;

namespace TravelPlannerApp.Application.Tests.Support;

internal static class ValidationTestHelper
{
    public static IReadOnlyList<ValidationResult> Validate(object instance)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(instance, new ValidationContext(instance), results, validateAllProperties: true);
        return results;
    }
}
