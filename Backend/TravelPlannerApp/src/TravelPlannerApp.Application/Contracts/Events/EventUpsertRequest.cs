using System.ComponentModel.DataAnnotations;
using TravelPlannerApp.Application.Common;
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
        var normalizedCurrencyCode = string.IsNullOrWhiteSpace(CurrencyCode) ? null : CurrencyCode.Trim().ToUpperInvariant();
        CurrencyCatalogEntry? currency = null;
        var hasSupportedCurrency = normalizedCurrencyCode is not null && CurrencyCatalog.TryGet(normalizedCurrencyCode, out currency);

        if (EndDateTime <= StartDateTime)
        {
            yield return new ValidationResult(
                "EndDateTime must be after StartDateTime.",
                [nameof(EndDateTime)]);
        }

        if (normalizedCurrencyCode is not null && normalizedCurrencyCode.Length != 3)
        {
            yield return new ValidationResult(
                "CurrencyCode must be a valid ISO 4217 currency code.",
                [nameof(CurrencyCode)]);
        }

        if (Cost is > 0 && string.IsNullOrWhiteSpace(CurrencyCode))
        {
            yield return new ValidationResult(
                "CurrencyCode is required when Cost is set.",
                [nameof(CurrencyCode)]);
        }

        if (normalizedCurrencyCode is not null && !hasSupportedCurrency)
        {
            yield return new ValidationResult(
                "CurrencyCode must be a supported ISO 4217 currency code.",
                [nameof(CurrencyCode)]);
        }

        if (Cost is > 0 && currency is not null && !CurrencyCatalog.SupportsAmount(Cost.Value, normalizedCurrencyCode))
        {
            yield return new ValidationResult(
                $"Cost supports up to {currency.MinorUnit} decimal place{(currency.MinorUnit == 1 ? string.Empty : "s")} for {currency.Code}.",
                [nameof(Cost)]);
        }
    }
}
