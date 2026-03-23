using NoorLocator.Application.Centers.Dtos;
using NoorLocator.Application.Validation;

namespace NoorLocator.Application.Centers.Validators;

public class CenterLocationQueryValidator : IValidator<CenterLocationQueryDto>
{
    public ValidationResult Validate(CenterLocationQueryDto instance)
    {
        var errors = new List<string>();
        CenterValidationRules.ValidateOptionalCoordinates(instance, errors);
        return errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Failure(errors.ToArray());
    }
}
