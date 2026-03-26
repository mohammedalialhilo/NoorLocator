using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NoorLocator.Infrastructure.Services.Media;

namespace NoorLocator.IntegrationTests;

public class DeploymentConfigurationTests(NoorLocatorWebApplicationFactory factory) : IClassFixture<NoorLocatorWebApplicationFactory>
{
    [Fact]
    public async Task RuntimeConfigEndpoint_NormalizesConfiguredApiBaseUrl()
    {
        using var configuredFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Frontend:ApiBaseUrl"] = "https://api.noorlocator.example/api/"
                });
            });
        });
        using var client = configuredFactory.CreateClient();

        var response = await client.GetAsync("/js/runtime-config.js");
        var script = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/javascript; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        Assert.Contains("apiBaseUrl", script);
        Assert.Contains("https://api.noorlocator.example", script);
        Assert.DoesNotContain("https://api.noorlocator.example/api", script);
        Assert.Contains("no-store", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public void AzureBlobStorageProviderConfiguration_ResolvesAzureBlobStorageService()
    {
        using var configuredFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["MediaStorage:Provider"] = "AzureBlob",
                    ["AzureBlobStorage:ServiceUri"] = "https://noorstorage.blob.core.windows.net",
                    ["AzureBlobStorage:ContainerName"] = "uploads"
                });
            });
        });

        using var scope = configuredFactory.Services.CreateScope();
        var mediaStorageService = scope.ServiceProvider.GetRequiredService<IMediaStorageService>();

        Assert.IsType<AzureBlobStorageService>(mediaStorageService);
    }
}
