using System.Security.Cryptography;

namespace NoorLocator.Infrastructure.Services.Media;

public static class MediaStorageFileNameGenerator
{
    public static string GenerateSafeImageFileName(string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName)?.Trim().ToLowerInvariant() ?? string.Empty;
        return $"{DateTime.UtcNow:yyyyMMddHHmmss}-{RandomNumberGenerator.GetHexString(20).ToLowerInvariant()}{extension}";
    }

    public static string NormalizeCategory(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            throw new ArgumentException("A storage category is required.", nameof(category));
        }

        return string.Join(
            '/',
            category
                .Replace('\\', '/')
                .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }
}
