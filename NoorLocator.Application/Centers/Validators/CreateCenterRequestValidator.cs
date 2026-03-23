using NoorLocator.Application.Centers.Dtos;
using NoorLocator.Application.Validation;

namespace NoorLocator.Application.Centers.Validators;

public class CreateCenterRequestValidator : IValidator<CreateCenterRequestDto>
{
    public ValidationResult Validate(CreateCenterRequestDto instance)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(instance.Name))
        {
            errors.Add("Center name is required.");
        }

        if (string.IsNullOrWhiteSpace(instance.City))
        {
            errors.Add("City is required.");
        }

        if (string.IsNullOrWhiteSpace(instance.Country))
        {
            errors.Add("Country is required.");
        }

        return errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Failure(errors.ToArray());
    }
}
