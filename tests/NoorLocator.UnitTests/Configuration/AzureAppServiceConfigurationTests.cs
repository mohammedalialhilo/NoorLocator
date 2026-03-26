using Microsoft.Extensions.Configuration;

namespace NoorLocator.UnitTests.Configuration;

public class AzureAppServiceConfigurationTests
{
    private static readonly Lock TestLock = new();

    [Fact]
    public void EnvironmentConfiguration_MapsMysqlConnectionStringPrefix_ToConnectionStrings()
    {
        lock (TestLock)
        {
            const string connectionString = "Server=noor-mysql.mysql.database.azure.com;Port=3306;Database=Noorlocator;User=noor;Password=secret;";
            const string key = "MYSQLCONNSTR_DefaultConnection";

            var originalValue = Environment.GetEnvironmentVariable(key);
            try
            {
                Environment.SetEnvironmentVariable(key, connectionString);

                var configuration = new ConfigurationBuilder()
                    .AddEnvironmentVariables()
                    .Build();

                Assert.Equal(connectionString, configuration.GetConnectionString("DefaultConnection"));
            }
            finally
            {
                Environment.SetEnvironmentVariable(key, originalValue);
            }
        }
    }

    [Fact]
    public void EnvironmentConfiguration_MapsCustomConnectionStringPrefix_ToConnectionStrings()
    {
        lock (TestLock)
        {
            const string connectionString = "DefaultEndpointsProtocol=https;AccountName=noorstorage;AccountKey=secret;EndpointSuffix=core.windows.net";
            const string key = "CUSTOMCONNSTR_AzureBlobStorage";

            var originalValue = Environment.GetEnvironmentVariable(key);
            try
            {
                Environment.SetEnvironmentVariable(key, connectionString);

                var configuration = new ConfigurationBuilder()
                    .AddEnvironmentVariables()
                    .Build();

                Assert.Equal(connectionString, configuration.GetConnectionString("AzureBlobStorage"));
            }
            finally
            {
                Environment.SetEnvironmentVariable(key, originalValue);
            }
        }
    }
}
