using System.Net.Mail;
using NoorLocator.Application.Admin.Dtos;
using NoorLocator.Application.Common.Localization;
using NoorLocator.Application.Validation;

namespace NoorLocator.Application.Admin.Validators;

public class UpdateAdminUserValidator : IValidator<UpdateAdminUserDto>
{
    public ValidationResult Validate(UpdateAdminUserDto instance)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(instance.Name))
        {
            errors.Add("Display name is required.");
        }
        else if (instance.Name.Trim().Length > 150)
        {
            errors.Add("Display name must be 150 characters or fewer.");
        }

        if (string.IsNullOrWhiteSpace(instance.Email))
        {
            errors.Add("Email is required.");
        }
        else if (instance.Email.Trim().Length > 256)
        {
            errors.Add("Email must be 256 characters or fewer.");
        }
        else if (!IsValidEmail(instance.Email))
        {
            errors.Add("A valid email address is required.");
        }

        if (!SupportedLanguageCatalog.IsSupported(instance.PreferredLanguageCode))
        {
            errors.Add("Preferred language must be one of the supported NoorLocator language codes.");
        }

        return errors.Count == 0
            ? ValidationResult.Success()
            : ValidationResult.Failure(errors.ToArray());
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var address = new MailAddress(email.Trim());
            return string.Equals(address.Address, email.Trim(), StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
