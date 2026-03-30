using System.ComponentModel.DataAnnotations;
using TravelPlannerApp.Domain.Enums;

namespace TravelPlannerApp.Application.Contracts.Events;

public abstract class EventUpsertRequest : IValidatableObject
{
    [Required]
    [StringLength(160, MinimumLength = 2)]
    public string Title { get; set; } = string.Empty;

    [StringLength(4000)]
    public string? Description { get; set; }

    [Required]
    public EventCategory Category { get; set; } = EventCategory.Other;

    [StringLength(32)]
    public string? Color { get; set; }

    [Required]
    public DateTime StartDateTime { get; set; }

    [Required]
    public DateTime EndDateTime { get; set; }

    [Required]
    [StringLength(120, MinimumLength = 2)]
    public string Timezone { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Location { get; set; }

    [StringLength(400)]
    public string? LocationAddress { get; set; }

    [Range(-90, 90)]
    public decimal? LocationLat { get; set; }

    [Range(-180, 180)]
    public decimal? LocationLng { get; set; }

    [Range(0, 1000000)]
    public decimal? Cost { get; set; }

    [StringLength(3, MinimumLength = 3)]
    public string? CurrencyCode { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndDateTime <= StartDateTime)
        {
            yield return new ValidationResult(
                "EndDateTime must be after StartDateTime.",
                [nameof(EndDateTime)]);
        }

        if (!string.IsNullOrWhiteSpace(CurrencyCode) && CurrencyCode.Trim().Length != 3)
        {
            yield return new ValidationResult(
                "CurrencyCode must be a valid ISO 4217 currency code.",
                [nameof(CurrencyCode)]);
        }
    }
}
