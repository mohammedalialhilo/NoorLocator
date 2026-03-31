using NoorLocator.Application.Notifications.Dtos;
using NoorLocator.Application.Validation;

namespace NoorLocator.Application.Notifications.Validators;

public class UpdateNotificationPreferencesValidator : IValidator<UpdateNotificationPreferencesDto>
{
    public ValidationResult Validate(UpdateNotificationPreferencesDto instance) => ValidationResult.Success();
}
