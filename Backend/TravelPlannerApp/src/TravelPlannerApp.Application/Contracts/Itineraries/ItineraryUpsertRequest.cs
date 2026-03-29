using System.ComponentModel.DataAnnotations;

namespace TravelPlannerApp.Application.Contracts.Itineraries;

public abstract class ItineraryUpsertRequest : IValidatableObject
{
    [Required]
    [StringLength(160, MinimumLength = 2)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    [Required]
    [StringLength(160, MinimumLength = 2)]
    public string Destination { get; set; } = string.Empty;

    [Required]
    public DateOnly StartDate { get; set; }

    [Required]
    public DateOnly EndDate { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndDate < StartDate)
        {
            yield return new ValidationResult(
                "EndDate must be on or after StartDate.",
                [nameof(EndDate)]);
        }
    }
}
