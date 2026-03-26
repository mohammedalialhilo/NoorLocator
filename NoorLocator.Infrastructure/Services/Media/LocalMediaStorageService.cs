using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NoorLocator.Application.Common.Configuration;
using NoorLocator.Application.Common.Models;

namespace NoorLocator.Infrastructure.Services.Media;

public class LocalMediaStorageService(
    IHostEnvironment hostEnvironment,
    IOptions<MediaStorageSettings> mediaStorageOptions) : IMediaStorageService
{
    private readonly MediaStorageSettings mediaStorageSettings = mediaStorageOptions.Value;

    public async Task<OperationResult<StoredMediaFile>> SaveImageAsync(
        UploadFilePayload file,
        string category,
        CancellationToken cancellationToken = default)
    {
        var validation = MediaImageValidation.Validate(file, mediaStorageSettings.MaxImageSizeBytes);
        if (!validation.Succeeded)
        {
            return OperationResult<StoredMediaFile>.Failure(validation.Message, validation.StatusCode);
        }

        var normalizedCategory = MediaStorageFileNameGenerator.NormalizeCategory(category);
        var safeFileName = MediaStorageFileNameGenerator.GenerateSafeImageFileName(file.FileName);

        var rootPath = ResolveStorageRootPath();
        var categoryFolder = Path.Combine(rootPath, normalizedCategory.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(categoryFolder);

        var filePath = Path.Combine(categoryFolder, safeFileName);
        var extension = Path.GetExtension(safeFileName)?.Trim().ToLowerInvariant() ?? string.Empty;

        await File.WriteAllBytesAsync(filePath, file.Content, cancellationToken);

        var normalizedBasePath = MediaStoragePathResolver.NormalizePublicBasePath(mediaStorageSettings.PublicBasePath);
        return OperationResult<StoredMediaFile>.Success(
            new StoredMediaFile
            {
                PublicUrl = $"{normalizedBasePath}/{normalizedCategory}/{safeFileName}",
                ContentType = MediaImageValidation.GetContentType(extension),
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

        var normalizedBasePath = MediaStoragePathResolver.NormalizePublicBasePath(mediaStorageSettings.PublicBasePath);
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
        return MediaStoragePathResolver.ResolveStorageRootPath(hostEnvironment, mediaStorageSettings);
    }

}
