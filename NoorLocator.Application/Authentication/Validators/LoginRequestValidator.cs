using NoorLocator.Application.Authentication.Dtos;
using NoorLocator.Application.Validation;

namespace NoorLocator.Application.Authentication.Validators;

public class LoginRequestValidator : IValidator<LoginRequestDto>
{
    public ValidationResult Validate(LoginRequestDto instance)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(instance.Email))
        {
            errors.Add("Email is required.");
        }

        if (string.IsNullOrWhiteSpace(instance.Password))
        {
            errors.Add("Password is required.");
        }

        return errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Failure(errors.ToArray());
    }
}
