using System.ComponentModel.DataAnnotations;

namespace TravelPlannerApp.Application.Common.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class PasswordPolicyAttribute : ValidationAttribute
{
    public const int MinimumLength = 8;
    public const int MaximumLength = 128;

    public PasswordPolicyAttribute()
        : base($"Password must be {MinimumLength}-{MaximumLength} characters and include at least one uppercase letter, one lowercase letter, one number, and one special character.")
    {
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
        {
            return ValidationResult.Success;
        }

        IEnumerable<string>? memberNames = string.IsNullOrWhiteSpace(validationContext.MemberName)
            ? null
            : [validationContext.MemberName];

        if (value is not string password)
        {
            return new ValidationResult(ErrorMessage, memberNames);
        }

        if (password.Length < MinimumLength || password.Length > MaximumLength)
        {
            return new ValidationResult(ErrorMessage, memberNames);
        }

        var hasUppercase = password.Any(char.IsUpper);
        var hasLowercase = password.Any(char.IsLower);
        var hasDigit = password.Any(char.IsDigit);
        var hasSpecialCharacter = password.Any(character => !char.IsLetterOrDigit(character));

        return hasUppercase && hasLowercase && hasDigit && hasSpecialCharacter
            ? ValidationResult.Success
            : new ValidationResult(ErrorMessage, memberNames);
    }
}
