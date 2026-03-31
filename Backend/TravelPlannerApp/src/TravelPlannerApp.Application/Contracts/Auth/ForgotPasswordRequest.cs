using System.ComponentModel.DataAnnotations;

namespace TravelPlannerApp.Application.Contracts.Auth;

public sealed class ForgotPasswordRequest
{
    [Required]
    [EmailAddress]
    [StringLength(200)]
    public string Email { get; set; } = string.Empty;
}
