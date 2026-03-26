using Microsoft.Extensions.Hosting;
using NoorLocator.Application.Common.Configuration;

namespace NoorLocator.Infrastructure.Services.Media;

public static class MediaStoragePathResolver
{
    public static string ResolveStorageRootPath(IHostEnvironment hostEnvironment, MediaStorageSettings settings)
    {
        var candidates = new[]
        {
            settings.RelativeRootPath,
            "uploads",
            "frontend/uploads",
            "../frontend/uploads"
        };

        foreach (var candidate in candidates.Where(candidate => !string.IsNullOrWhiteSpace(candidate)))
        {
            var fullPath = Path.IsPathRooted(candidate)
                ? Path.GetFullPath(candidate)
                : Path.GetFullPath(Path.Combine(hostEnvironment.ContentRootPath, candidate!));

            try
            {
                Directory.CreateDirectory(fullPath);
                return fullPath;
            }
            catch (Exception)
            {
                continue;
            }
        }

        var fallback = Path.GetFullPath(Path.Combine(hostEnvironment.ContentRootPath, "uploads"));
        Directory.CreateDirectory(fallback);
        return fallback;
    }

    public static string NormalizePublicBasePath(string publicBasePath)
    {
        var trimmed = string.IsNullOrWhiteSpace(publicBasePath) ? "/uploads" : publicBasePath.Trim();
        if (!trimmed.StartsWith('/'))
        {
            trimmed = $"/{trimmed}";
        }

        return trimmed.TrimEnd('/');
    }
}
