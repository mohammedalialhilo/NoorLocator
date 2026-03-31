using NoorLocator.Application.Authentication.Dtos;
using NoorLocator.Application.Validation;

namespace NoorLocator.Application.Authentication.Validators;

public class ResetPasswordRequestValidator : IValidator<ResetPasswordRequestDto>
{
    public ValidationResult Validate(ResetPasswordRequestDto instance)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(instance.Token))
        {
            errors.Add("Reset token is required.");
        }

        if (string.IsNullOrWhiteSpace(instance.Password) || instance.Password.Length < 8)
        {
            errors.Add("Password must be at least 8 characters.");
        }

        if (!string.Equals(instance.Password, instance.ConfirmPassword, StringComparison.Ordinal))
        {
            errors.Add("Passwords do not match.");
        }

        return errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Failure(errors.ToArray());
    }
}
