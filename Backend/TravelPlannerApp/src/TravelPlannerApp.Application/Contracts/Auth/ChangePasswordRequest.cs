using System.ComponentModel.DataAnnotations;
using TravelPlannerApp.Application.Common.Validation;

namespace TravelPlannerApp.Application.Contracts.Auth;

public sealed class ChangePasswordRequest
{
    [Required]
    [StringLength(128, MinimumLength = 8)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [PasswordPolicy]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [Compare(nameof(NewPassword))]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
