using System.ComponentModel.DataAnnotations;
using TravelPlannerApp.Application.Common.Validation;

namespace TravelPlannerApp.Application.Contracts.Auth;

public sealed class ResetPasswordRequest
{
    [Required]
    [StringLength(512, MinimumLength = 32)]
    public string Token { get; set; } = string.Empty;

    [Required]
    [PasswordPolicy]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [Compare(nameof(NewPassword))]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
