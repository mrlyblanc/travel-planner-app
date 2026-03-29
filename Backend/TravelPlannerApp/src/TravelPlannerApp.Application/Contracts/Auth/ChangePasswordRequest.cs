using System.ComponentModel.DataAnnotations;

namespace TravelPlannerApp.Application.Contracts.Auth;

public sealed class ChangePasswordRequest
{
    [Required]
    [StringLength(200, MinimumLength = 8)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [StringLength(200, MinimumLength = 8)]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [Compare(nameof(NewPassword))]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
