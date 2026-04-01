namespace NoorLocator.Application.Common.Localization;

public static class SupportedLanguageCatalog
{
    public const string FallbackLanguageCode = "en";

    private static readonly HashSet<string> SupportedCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "ar",
        "fa",
        "da",
        "de",
        "es",
        "sv",
        "pt",
        FallbackLanguageCode
    };

    public static IReadOnlyCollection<string> Codes => SupportedCodes.OrderBy(code => code, StringComparer.OrdinalIgnoreCase).ToArray();

    public static bool IsSupported(string? code)
        => !string.IsNullOrWhiteSpace(code) && SupportedCodes.Contains(code.Trim().ToLowerInvariant());

    public static string NormalizeOrFallback(string? code)
        => IsSupported(code) ? code!.Trim().ToLowerInvariant() : FallbackLanguageCode;

    public static bool IsRightToLeft(string? code)
    {
        var normalized = NormalizeOrFallback(code);
        return string.Equals(normalized, "ar", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, "fa", StringComparison.OrdinalIgnoreCase);
    }
}
