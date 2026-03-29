using System.ComponentModel.DataAnnotations;

namespace TravelPlannerApp.Application.Contracts.Itineraries;

public sealed class ReplaceItineraryMembersRequest : IValidatableObject
{
    [Required]
    public List<string> UserIds { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (UserIds.Count == 0)
        {
            yield return new ValidationResult("At least one member is required.", [nameof(UserIds)]);
            yield break;
        }

        if (UserIds.Any(static userId => string.IsNullOrWhiteSpace(userId)))
        {
            yield return new ValidationResult("Member user ids cannot be blank.", [nameof(UserIds)]);
        }
    }
}
