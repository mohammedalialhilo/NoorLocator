using NoorLocator.Application.Common.Localization;
using NoorLocator.Application.Profile.Dtos;
using NoorLocator.Application.Validation;

namespace NoorLocator.Application.Profile.Validators;

public class UpdatePreferredLanguageValidator : IValidator<UpdatePreferredLanguageDto>
{
    public ValidationResult Validate(UpdatePreferredLanguageDto instance)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(instance.PreferredLanguageCode))
        {
            errors.Add("Preferred language is required.");
        }
        else if (!SupportedLanguageCatalog.IsSupported(instance.PreferredLanguageCode))
        {
            errors.Add("Preferred language is not supported.");
        }

        return errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Failure(errors.ToArray());
    }
}
