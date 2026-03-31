using System.ComponentModel.DataAnnotations;

namespace TravelPlannerApp.Application.Contracts.Itineraries;

public sealed class JoinItineraryByCodeRequest : IValidatableObject
{
    [Required]
    [StringLength(5, MinimumLength = 5)]
    public string Code { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Code) || Code.Length != 5 || !Code.All(char.IsDigit))
        {
            yield return new ValidationResult("Share code must be a 5-digit code.", [nameof(Code)]);
        }
    }
}
