using NoorLocator.Application.Management.Dtos;
using NoorLocator.Application.Validation;

namespace NoorLocator.Application.Management.Validators;

public class ManagerRequestValidator : IValidator<ManagerRequestDto>
{
    public ValidationResult Validate(ManagerRequestDto instance)
    {
        return instance.CenterId > 0
            ? ValidationResult.Success()
            : ValidationResult.Failure("CenterId must be greater than zero.");
    }
}
