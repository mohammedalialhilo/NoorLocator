using NoorLocator.Application.Centers.Dtos;
using NoorLocator.Application.Validation;

namespace NoorLocator.Application.Centers.Validators;

public class CenterSearchQueryValidator : IValidator<CenterSearchQueryDto>
{
    public ValidationResult Validate(CenterSearchQueryDto instance)
    {
        var errors = new List<string>();

        CenterValidationRules.ValidateOptionalCoordinates(instance, errors);
        CenterValidationRules.ValidateSearchTextLength(instance.Query, 200, "Query", errors);
        CenterValidationRules.ValidateSearchTextLength(instance.City, 100, "City", errors);
        CenterValidationRules.ValidateSearchTextLength(instance.Country, 100, "Country", errors);
        CenterValidationRules.ValidateLanguageCode(instance.LanguageCode, errors);

        if (string.IsNullOrWhiteSpace(instance.Query) &&
            string.IsNullOrWhiteSpace(instance.City) &&
            string.IsNullOrWhiteSpace(instance.Country) &&
            string.IsNullOrWhiteSpace(instance.LanguageCode))
        {
            errors.Add("Provide at least one search filter.");
        }

        return errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Failure(errors.ToArray());
    }
}
