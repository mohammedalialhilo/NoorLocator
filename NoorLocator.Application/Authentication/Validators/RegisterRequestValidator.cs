using NoorLocator.Application.Authentication.Dtos;
using NoorLocator.Application.Validation;

namespace NoorLocator.Application.Authentication.Validators;

public class RegisterRequestValidator : IValidator<RegisterRequestDto>
{
    public ValidationResult Validate(RegisterRequestDto instance)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(instance.Name))
        {
            errors.Add("Name is required.");
        }

        if (string.IsNullOrWhiteSpace(instance.Email))
        {
            errors.Add("Email is required.");
        }

        if (string.IsNullOrWhiteSpace(instance.Password) || instance.Password.Length < 8)
        {
            errors.Add("Password must be at least 8 characters.");
        }

        return errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Failure(errors.ToArray());
    }
}
