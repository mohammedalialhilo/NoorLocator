using System.Security.Cryptography;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NoorLocator.Application.Common.Configuration;
using NoorLocator.Application.Common.Models;

namespace NoorLocator.Infrastructure.Services.Media;

public class LocalMediaStorageService(
    IHostEnvironment hostEnvironment,
    IOptions<MediaStorageSettings> mediaStorageOptions) : IMediaStorageService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

    private readonly MediaStorageSettings mediaStorageSettings = mediaStorageOptions.Value;

    public async Task<OperationResult<StoredMediaFile>> SaveImageAsync(
        UploadFilePayload file,
        string category,
        CancellationToken cancellationToken = default)
    {
        if (file.Content.Length == 0)
        {
            return OperationResult<StoredMediaFile>.Failure("An image file is required.", 400);
        }

        if (file.Content.Length > mediaStorageSettings.MaxImageSizeBytes)
        {
            return OperationResult<StoredMediaFile>.Failure("Image files must be 5MB or smaller.", 400);
        }

        var extension = Path.GetExtension(file.FileName)?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!AllowedExtensions.Contains(extension))
        {
            return OperationResult<StoredMediaFile>.Failure("Only JPG, JPEG, PNG, and WEBP files are allowed.", 400);
        }

        if (!MatchesKnownImageSignature(extension, file.Content))
        {
            return OperationResult<StoredMediaFile>.Failure("The uploaded file content is not a supported image.", 400);
        }

        var rootPath = ResolveStorageRootPath();
        var categoryFolder = Path.Combine(rootPath, category);
        Directory.CreateDirectory(categoryFolder);

        var safeFileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}-{RandomNumberGenerator.GetHexString(20).ToLowerInvariant()}{extension}";
        var filePath = Path.Combine(categoryFolder, safeFileName);

        await File.WriteAllBytesAsync(filePath, file.Content, cancellationToken);

        var normalizedBasePath = NormalizePublicBasePath(mediaStorageSettings.PublicBasePath);
        return OperationResult<StoredMediaFile>.Success(
            new StoredMediaFile
            {
                PublicUrl = $"{normalizedBasePath}/{category}/{safeFileName}",
                ContentType = GetContentType(extension),
                SizeBytes = file.Content.LongLength
            },
            "Image stored successfully.",
            201);
    }

    public Task DeleteFileAsync(string? publicUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(publicUrl))
        {
            return Task.CompletedTask;
        }

        var normalizedBasePath = NormalizePublicBasePath(mediaStorageSettings.PublicBasePath);
        if (!publicUrl.StartsWith($"{normalizedBasePath}/", StringComparison.OrdinalIgnoreCase))
        {
            return Task.CompletedTask;
        }

        var relativePath = publicUrl[normalizedBasePath.Length..].TrimStart('/')
            .Replace('/', Path.DirectorySeparatorChar);

        var rootPath = ResolveStorageRootPath();
        var fullPath = Path.GetFullPath(Path.Combine(rootPath, relativePath));
        var normalizedRootPath = Path.GetFullPath(rootPath);

        if (!fullPath.StartsWith(normalizedRootPath, StringComparison.OrdinalIgnoreCase))
        {
            return Task.CompletedTask;
        }

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    private string ResolveStorageRootPath()
    {
        var candidates = new[]
        {
            mediaStorageSettings.RelativeRootPath,
            "frontend/uploads",
            "../frontend/uploads"
        };

        foreach (var candidate in candidates.Where(candidate => !string.IsNullOrWhiteSpace(candidate)))
        {
            var fullPath = Path.IsPathRooted(candidate)
                ? Path.GetFullPath(candidate)
                : Path.GetFullPath(Path.Combine(hostEnvironment.ContentRootPath, candidate));

            var parentDirectory = Directory.Exists(fullPath)
                ? fullPath
                : Path.GetDirectoryName(fullPath);

            if (string.IsNullOrWhiteSpace(parentDirectory) || !Directory.Exists(parentDirectory))
            {
                continue;
            }

            Directory.CreateDirectory(fullPath);
            return fullPath;
        }

        var fallback = Path.GetFullPath(Path.Combine(hostEnvironment.ContentRootPath, "frontend/uploads"));
        Directory.CreateDirectory(fallback);
        return fallback;
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

    private static string NormalizePublicBasePath(string publicBasePath)
    {
        var trimmed = string.IsNullOrWhiteSpace(publicBasePath) ? "/uploads" : publicBasePath.Trim();
        if (!trimmed.StartsWith('/'))
        {
            trimmed = $"/{trimmed}";
        }

        return trimmed.TrimEnd('/');
    }

    private static string GetContentType(string extension)
    {
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }
}
