using NoorLocator.Application.EventAnnouncements.Dtos;
using NoorLocator.Application.Validation;

namespace NoorLocator.Application.EventAnnouncements.Validators;

public class UpdateEventAnnouncementValidator : IValidator<UpdateEventAnnouncementDto>
{
    public ValidationResult Validate(UpdateEventAnnouncementDto instance)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(instance.Title))
        {
            errors.Add("Announcement title is required.");
        }

        if (instance.CenterId <= 0)
        {
            errors.Add("CenterId must be provided.");
        }

        if (!Enum.IsDefined(instance.Status))
        {
            errors.Add("Announcement status is invalid.");
        }

        return errors.Count == 0
            ? ValidationResult.Success()
            : ValidationResult.Failure(errors.ToArray());
    }
}
