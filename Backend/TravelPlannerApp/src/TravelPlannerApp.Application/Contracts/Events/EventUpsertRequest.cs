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

    [StringLength(4000)]
    public string? Remarks { get; set; }

    [Required]
    public EventCategory Category { get; set; } = EventCategory.Other;

    [StringLength(32)]
    public string? Color { get; set; }

    public bool IsAllDay { get; set; }

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

    public List<EventLinkInput> Links { get; set; } = [];

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

        if (IsAllDay)
        {
            var expectedEndTime = new TimeSpan(23, 59, 0);

            if (StartDateTime.TimeOfDay != TimeSpan.Zero
                || EndDateTime.TimeOfDay != expectedEndTime
                || EndDateTime.Date < StartDateTime.Date)
            {
                yield return new ValidationResult(
                    "All-day events must use a full-day time window from 12:00 AM to 11:59 PM and may span one or more dates.",
                    [nameof(IsAllDay), nameof(StartDateTime), nameof(EndDateTime)]);
            }
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

        if (Links.Count > 12)
        {
            yield return new ValidationResult(
                "Links supports up to 12 entries per event.",
                [nameof(Links)]);
        }

        for (var index = 0; index < Links.Count; index++)
        {
            var link = Links[index];
            var description = link.Description?.Trim() ?? string.Empty;
            var url = link.Url?.Trim() ?? string.Empty;

            if (description.Length is < 2 or > 160)
            {
                yield return new ValidationResult(
                    "Link descriptions must be between 2 and 160 characters.",
                    [$"{nameof(Links)}[{index}].{nameof(EventLinkInput.Description)}"]);
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                yield return new ValidationResult(
                    "Link URLs must be valid absolute http or https URLs.",
                    [$"{nameof(Links)}[{index}].{nameof(EventLinkInput.Url)}"]);
            }
        }
    }
}
