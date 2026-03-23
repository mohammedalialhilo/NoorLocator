using NoorLocator.Application.Majalis.Dtos;
using NoorLocator.Application.Validation;

namespace NoorLocator.Application.Majalis.Validators;

public class UpdateMajlisValidator : IValidator<UpdateMajlisDto>
{
    public ValidationResult Validate(UpdateMajlisDto instance)
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

        if (string.IsNullOrWhiteSpace(instance.Time))
        {
            errors.Add("Majlis time is required.");
        }

        return errors.Count == 0
            ? ValidationResult.Success()
            : ValidationResult.Failure(errors.ToArray());
    }
}
