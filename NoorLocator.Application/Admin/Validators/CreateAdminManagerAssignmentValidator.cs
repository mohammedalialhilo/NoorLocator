using NoorLocator.Application.Admin.Dtos;
using NoorLocator.Application.Validation;

namespace NoorLocator.Application.Admin.Validators;

public class CreateAdminManagerAssignmentValidator : IValidator<CreateAdminManagerAssignmentDto>
{
    public ValidationResult Validate(CreateAdminManagerAssignmentDto instance)
    {
        var errors = new List<string>();

        if (instance.UserId <= 0)
        {
            errors.Add("A valid user must be selected.");
        }

        if (instance.CenterId <= 0)
        {
            errors.Add("A valid center must be selected.");
        }

        return errors.Count == 0
            ? ValidationResult.Success()
            : ValidationResult.Failure(errors.ToArray());
    }
}
