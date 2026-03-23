using NoorLocator.Application.Centers.Dtos;
using NoorLocator.Application.Validation;

namespace NoorLocator.Application.Centers.Validators;

public class UpdateCenterValidator : IValidator<UpdateCenterDto>
{
    public ValidationResult Validate(UpdateCenterDto instance)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(instance.Name))
        {
            errors.Add("Center name is required.");
        }

        if (string.IsNullOrWhiteSpace(instance.Address))
        {
            errors.Add("Center address is required.");
        }

        if (string.IsNullOrWhiteSpace(instance.City))
        {
            errors.Add("City is required.");
        }

        if (string.IsNullOrWhiteSpace(instance.Country))
        {
            errors.Add("Country is required.");
        }

        if (instance.Latitude is < -90 or > 90)
        {
            errors.Add("Latitude must be between -90 and 90.");
        }

        if (instance.Longitude is < -180 or > 180)
        {
            errors.Add("Longitude must be between -180 and 180.");
        }

        return errors.Count == 0
            ? ValidationResult.Success()
            : ValidationResult.Failure(errors.ToArray());
    }
}
