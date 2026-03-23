using NoorLocator.Application.Majalis.Dtos;
using NoorLocator.Application.Validation;

namespace NoorLocator.Application.Majalis.Validators;

public class CreateMajlisValidator : IValidator<CreateMajlisDto>
{
    public ValidationResult Validate(CreateMajlisDto instance)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(instance.Title))
        {
            errors.Add("Majlis title is required.");
        }

        if (instance.CenterId <= 0)
        {
            errors.Add("CenterId must be provided.");
        }

        if (instance.Date == default)
        {
            errors.Add("A valid majlis date is required.");
        }

        return errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Failure(errors.ToArray());
    }
}
