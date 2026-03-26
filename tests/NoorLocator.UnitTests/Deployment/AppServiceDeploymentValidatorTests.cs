using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using NoorLocator.Application.Common.Configuration;
using NoorLocator.Infrastructure.Deployment;

namespace NoorLocator.UnitTests.Deployment;

public class AppServiceDeploymentValidatorTests
{
    [Fact]
    public void Validate_WhenNotRunningInAppService_DoesNotThrow()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        var environment = CreateEnvironment("Production");
        var settings = new MediaStorageSettings
        {
            Provider = "Local",
            RelativeRootPath = "uploads"
        };

        AppServiceDeploymentValidator.Validate(configuration, environment, settings);
    }

    [Fact]
    public void Validate_WhenRunningInAppServiceWithAzureBlobProvider_DoesNotThrow()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["WEBSITE_SITE_NAME"] = "noorlocator-prod",
                ["WEBSITE_RUN_FROM_PACKAGE"] = "1"
            })
            .Build();
        var environment = CreateEnvironment("Production");
        var settings = new MediaStorageSettings
        {
            Provider = "AzureBlob"
        };

        AppServiceDeploymentValidator.Validate(configuration, environment, settings);
    }

    [Fact]
    public void Validate_WhenRunningInAppServiceWithRelativeLocalStorage_Throws()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["WEBSITE_SITE_NAME"] = "noorlocator-prod",
                ["WEBSITE_RUN_FROM_PACKAGE"] = "1",
                ["HOME"] = Path.Combine(Path.GetTempPath(), "noorlocator-home")
            })
            .Build();
        var environment = CreateEnvironment("Production");
        var settings = new MediaStorageSettings
        {
            Provider = "Local",
            RelativeRootPath = "uploads"
        };

        var exception = Assert.Throws<InvalidOperationException>(() =>
            AppServiceDeploymentValidator.Validate(configuration, environment, settings));

        Assert.Contains("MediaStorage:Provider=AzureBlob", exception.Message);
    }

    [Fact]
    public void Validate_WhenRunningInAppServiceWithWritableHomePath_DoesNotThrow()
    {
        var homePath = Path.Combine(Path.GetTempPath(), "noorlocator-home");
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["WEBSITE_SITE_NAME"] = "noorlocator-prod",
                ["WEBSITE_RUN_FROM_PACKAGE"] = "1",
                ["HOME"] = homePath
            })
            .Build();
        var environment = CreateEnvironment("Production");
        var settings = new MediaStorageSettings
        {
            Provider = "Local",
            RelativeRootPath = Path.Combine(homePath, "site", "data", "uploads")
        };

        AppServiceDeploymentValidator.Validate(configuration, environment, settings);
    }

    private static IHostEnvironment CreateEnvironment(string environmentName)
    {
        return new TestHostEnvironment
        {
            EnvironmentName = environmentName,
            ApplicationName = "NoorLocator",
            ContentRootPath = Directory.GetCurrentDirectory(),
            ContentRootFileProvider = new NullFileProvider()
        };
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;

        public string ApplicationName { get; set; } = string.Empty;

        public string ContentRootPath { get; set; } = string.Empty;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
