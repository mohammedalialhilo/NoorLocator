using NoorLocator.Application.Languages.Dtos;
using NoorLocator.Application.Validation;

namespace NoorLocator.Application.Languages.Validators;

public class CreateCenterLanguageSuggestionValidator : IValidator<CreateCenterLanguageSuggestionDto>
{
    public ValidationResult Validate(CreateCenterLanguageSuggestionDto instance)
    {
        var errors = new List<string>();

        if (instance.CenterId <= 0)
        {
            errors.Add("CenterId must be greater than zero.");
        }

        if (instance.LanguageId <= 0)
        {
            errors.Add("LanguageId must be greater than zero.");
        }

        return errors.Count == 0
            ? ValidationResult.Success()
            : ValidationResult.Failure(errors.ToArray());
    }
}
