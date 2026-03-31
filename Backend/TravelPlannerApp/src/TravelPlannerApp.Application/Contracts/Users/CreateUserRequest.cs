using System.ComponentModel.DataAnnotations;
using TravelPlannerApp.Application.Common.Validation;

namespace TravelPlannerApp.Application.Contracts.Users;

public sealed class CreateUserRequest
{
    [Required]
    [StringLength(120, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [PasswordPolicy]
    public string Password { get; set; } = string.Empty;

    [StringLength(16)]
    public string? Avatar { get; set; }
}
