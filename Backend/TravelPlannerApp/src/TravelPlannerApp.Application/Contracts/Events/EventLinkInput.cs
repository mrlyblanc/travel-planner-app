using System.ComponentModel.DataAnnotations;

namespace TravelPlannerApp.Application.Contracts.Events;

public sealed class EventLinkInput
{
    [Required]
    [StringLength(160, MinimumLength = 2)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [StringLength(2048, MinimumLength = 8)]
    public string Url { get; set; } = string.Empty;
}
