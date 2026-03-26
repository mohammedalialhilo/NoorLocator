using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NoorLocator.Application.Common.Configuration;
using NoorLocator.Infrastructure.Services.Media;

namespace NoorLocator.Infrastructure.Deployment;

public static class AppServiceDeploymentValidator
{
    public static void Validate(IConfiguration configuration, IHostEnvironment environment, MediaStorageSettings mediaStorageSettings)
    {
        if (!IsProductionLike(environment.EnvironmentName) || string.IsNullOrWhiteSpace(configuration["WEBSITE_SITE_NAME"]))
        {
            return;
        }

        var provider = string.IsNullOrWhiteSpace(mediaStorageSettings.Provider)
            ? MediaStorageProviders.Local
            : mediaStorageSettings.Provider.Trim();

        if (!provider.Equals(MediaStorageProviders.Local, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var relativeRootPath = mediaStorageSettings.RelativeRootPath;
        var homePath = configuration["HOME"];
        if (IsAllowedAppServiceLocalStoragePath(relativeRootPath, homePath))
        {
            return;
        }

        var deploymentModeHint = IsEnabled(configuration["WEBSITE_RUN_FROM_PACKAGE"])
            ? "WEBSITE_RUN_FROM_PACKAGE is enabled, so relative local paths resolve under the read-only app package."
            : "Local storage under the deployed app content root is fragile on App Service and is lost on redeploy.";

        var writableHomePath = string.IsNullOrWhiteSpace(homePath) ? "/home" : homePath.Trim();

        throw new InvalidOperationException(
            "MediaStorage:Provider=Local is not a safe Azure App Service production default. " +
            $"{deploymentModeHint} Use MediaStorage:Provider=AzureBlob, or set MediaStorage:RelativeRootPath to an absolute writable path under HOME ({writableHomePath}).");
    }

    public static bool IsAllowedAppServiceLocalStoragePath(string? configuredPath, string? homePath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath) || string.IsNullOrWhiteSpace(homePath) || !Path.IsPathRooted(configuredPath))
        {
            return false;
        }

        var normalizedConfiguredPath = NormalizeDirectoryPath(Path.GetFullPath(configuredPath.Trim()));
        var normalizedHomePath = NormalizeDirectoryPath(Path.GetFullPath(homePath.Trim()));

        return normalizedConfiguredPath.StartsWith(normalizedHomePath, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsProductionLike(string? environmentName)
    {
        return !string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase) &&
               !string.Equals(environmentName, "Testing", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsEnabled(string? value)
    {
        return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeDirectoryPath(string path)
    {
        return path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
    }
}
