using System.Text.RegularExpressions;
using NoorLocator.Application.Centers.Dtos;

namespace NoorLocator.Application.Centers.Validators;

internal static partial class CenterValidationRules
{
    public static void ValidateOptionalCoordinates(CenterLocationQueryDto instance, ICollection<string> errors)
    {
        if (instance.Lat.HasValue != instance.Lng.HasValue)
        {
            errors.Add("Latitude and longitude must be provided together.");
            return;
        }

        ValidateCoordinateRanges(instance.Lat, instance.Lng, errors);
    }

    public static void ValidateRequiredCoordinates(CenterLocationQueryDto instance, ICollection<string> errors)
    {
        if (!instance.Lat.HasValue || !instance.Lng.HasValue)
        {
            errors.Add("Latitude and longitude are required.");
            return;
        }

        ValidateCoordinateRanges(instance.Lat, instance.Lng, errors);
    }

    public static void ValidateSearchTextLength(string? value, int maxLength, string fieldName, ICollection<string> errors)
    {
        if (!string.IsNullOrWhiteSpace(value) && value.Trim().Length > maxLength)
        {
            errors.Add($"{fieldName} must be {maxLength} characters or fewer.");
        }
    }

    public static void ValidateLanguageCode(string? value, ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var normalized = value.Trim();
        if (normalized.Length is < 2 or > 10 || !LanguageCodeRegex().IsMatch(normalized))
        {
            errors.Add("Language code must contain 2 to 10 letters or hyphens.");
        }
    }

    private static void ValidateCoordinateRanges(decimal? lat, decimal? lng, ICollection<string> errors)
    {
        if (lat.HasValue && (lat.Value < -90m || lat.Value > 90m))
        {
            errors.Add("Latitude must be between -90 and 90.");
        }

        if (lng.HasValue && (lng.Value < -180m || lng.Value > 180m))
        {
            errors.Add("Longitude must be between -180 and 180.");
        }
    }

    [GeneratedRegex("^[a-zA-Z-]+$")]
    private static partial Regex LanguageCodeRegex();
}
