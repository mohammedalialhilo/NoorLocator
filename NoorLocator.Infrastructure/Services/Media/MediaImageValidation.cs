using NoorLocator.Application.Common.Models;

namespace NoorLocator.Infrastructure.Services.Media;

public static class MediaImageValidation
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

    public static OperationResult<object?> Validate(UploadFilePayload file, int maxImageSizeBytes)
    {
        if (file.Content.Length == 0)
        {
            return OperationResult<object?>.Failure("An image file is required.", 400);
        }

        if (file.Content.Length > maxImageSizeBytes)
        {
            return OperationResult<object?>.Failure("Image files must be 5MB or smaller.", 400);
        }

        var extension = Path.GetExtension(file.FileName)?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!AllowedExtensions.Contains(extension))
        {
            return OperationResult<object?>.Failure("Only JPG, JPEG, PNG, and WEBP files are allowed.", 400);
        }

        if (!MatchesKnownImageSignature(extension, file.Content))
        {
            return OperationResult<object?>.Failure("The uploaded file content is not a supported image.", 400);
        }

        return OperationResult<object?>.Success(null);
    }

    public static string GetContentType(string extension)
    {
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }

    private static bool MatchesKnownImageSignature(string extension, byte[] content)
    {
        return extension switch
        {
            ".jpg" or ".jpeg" => content.Length >= 3 &&
                                 content[0] == 0xFF &&
                                 content[1] == 0xD8 &&
                                 content[2] == 0xFF,
            ".png" => content.Length >= 8 &&
                      content[0] == 0x89 &&
                      content[1] == 0x50 &&
                      content[2] == 0x4E &&
                      content[3] == 0x47 &&
                      content[4] == 0x0D &&
                      content[5] == 0x0A &&
                      content[6] == 0x1A &&
                      content[7] == 0x0A,
            ".webp" => content.Length >= 12 &&
                       content[0] == (byte)'R' &&
                       content[1] == (byte)'I' &&
                       content[2] == (byte)'F' &&
                       content[3] == (byte)'F' &&
                       content[8] == (byte)'W' &&
                       content[9] == (byte)'E' &&
                       content[10] == (byte)'B' &&
                       content[11] == (byte)'P',
            _ => false
        };
    }
}
