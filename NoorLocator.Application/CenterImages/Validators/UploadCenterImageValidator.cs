using NoorLocator.Application.CenterImages.Dtos;
using NoorLocator.Application.Validation;

namespace NoorLocator.Application.CenterImages.Validators;

public class UploadCenterImageValidator : IValidator<UploadCenterImageDto>
{
    public ValidationResult Validate(UploadCenterImageDto instance)
    {
        if (instance.CenterId <= 0)
        {
            return ValidationResult.Failure(["CenterId must be provided."]);
        }

        return ValidationResult.Success();
    }
}
