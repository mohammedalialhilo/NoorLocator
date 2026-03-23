using NoorLocator.Application.Centers.Dtos;
using NoorLocator.Application.Validation;

namespace NoorLocator.Application.Centers.Validators;

public class NearestCentersQueryValidator : IValidator<NearestCentersQueryDto>
{
    public ValidationResult Validate(NearestCentersQueryDto instance)
    {
        var errors = new List<string>();
        CenterValidationRules.ValidateRequiredCoordinates(instance, errors);
        return errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Failure(errors.ToArray());
    }
}
