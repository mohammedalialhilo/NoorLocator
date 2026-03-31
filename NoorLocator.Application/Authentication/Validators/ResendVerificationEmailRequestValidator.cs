using NoorLocator.Application.Authentication.Dtos;
using NoorLocator.Application.Validation;

namespace NoorLocator.Application.Authentication.Validators;

public class ResendVerificationEmailRequestValidator : IValidator<ResendVerificationEmailRequestDto>
{
    public ValidationResult Validate(ResendVerificationEmailRequestDto instance)
    {
        var errors = new List<string>();

        if (!string.IsNullOrWhiteSpace(instance.Email) && instance.Email.Trim().Length > 256)
        {
            errors.Add("Email must be 256 characters or fewer.");
        }

        return errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Failure(errors.ToArray());
    }
}
