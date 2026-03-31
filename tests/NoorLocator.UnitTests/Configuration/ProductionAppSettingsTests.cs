using Microsoft.Extensions.Configuration;

namespace NoorLocator.UnitTests.Configuration;

public class ProductionAppSettingsTests
{
    [Fact]
    public void ProductionDefaults_EnableForwardedHeadersAndHttpsRedirection()
    {
        var configuration = LoadProductionConfiguration();

        Assert.True(configuration.GetValue<bool>("ReverseProxy:UseForwardedHeaders"));
        Assert.True(configuration.GetValue<bool>("Https:RedirectionEnabled"));
    }

    [Fact]
    public void ProductionDefaults_DisableSwaggerAndAutomaticMigrations()
    {
        var configuration = LoadProductionConfiguration();

        Assert.False(configuration.GetValue<bool>("Swagger:Enabled"));
        Assert.False(configuration.GetValue<bool>("Seeding:ApplyMigrations"));
        Assert.False(configuration.GetValue<bool>("Seeding:SeedDemoData"));
    }

    [Fact]
    public void ProductionDefaults_PreferAzureBlobStorage()
    {
        var configuration = LoadProductionConfiguration();

        Assert.Equal("AzureBlob", configuration["MediaStorage:Provider"]);
        Assert.Equal("/uploads", configuration["MediaStorage:PublicBasePath"]);
    }

    private static IConfigurationRoot LoadProductionConfiguration()
    {
        var repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var productionSettingsPath = Path.Combine(repositoryRoot, "NoorLocator.Api", "appsettings.Production.json");

        return new ConfigurationBuilder()
            .AddJsonFile(productionSettingsPath, optional: false, reloadOnChange: false)
            .Build();
    }
}
