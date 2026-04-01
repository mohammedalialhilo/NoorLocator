using NoorLocator.Application.Admin.Dtos;
using NoorLocator.Application.Validation;

namespace NoorLocator.Application.Admin.Validators;

public class UpdateAdminManagerAssignmentValidator : IValidator<UpdateAdminManagerAssignmentDto>
{
    public ValidationResult Validate(UpdateAdminManagerAssignmentDto instance)
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
