using System.ComponentModel.DataAnnotations;

namespace TravelPlannerApp.Application.Contracts.Auth;

public sealed class RefreshTokenRequest
{
    [Required]
    [StringLength(4000, MinimumLength = 32)]
    public string RefreshToken { get; set; } = string.Empty;
}
