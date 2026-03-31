using System.ComponentModel.DataAnnotations;

namespace TravelPlannerApp.Application.Contracts.Auth;

public sealed class LoginRequest
{
    [Required]
    [EmailAddress]
    [StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(128, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;
}
