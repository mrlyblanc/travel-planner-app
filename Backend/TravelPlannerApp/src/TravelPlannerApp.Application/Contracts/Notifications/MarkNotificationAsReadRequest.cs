using System.ComponentModel.DataAnnotations;

namespace TravelPlannerApp.Application.Contracts.Notifications;

public sealed class MarkNotificationAsReadRequest
{
    [Required]
    public DateTime ReadAtUtc { get; set; } = DateTime.UtcNow;
}
