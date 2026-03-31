using NoorLocator.Application.Authentication.Dtos;
using NoorLocator.Application.Validation;

namespace NoorLocator.Application.Authentication.Validators;

public class ForgotPasswordRequestValidator : IValidator<ForgotPasswordRequestDto>
{
    public ValidationResult Validate(ForgotPasswordRequestDto instance)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(instance.Email))
        {
            errors.Add("Email is required.");
        }

        return errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Failure(errors.ToArray());
    }
}
