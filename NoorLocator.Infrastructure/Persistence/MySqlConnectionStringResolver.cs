using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace NoorLocator.Infrastructure.Persistence;

public static class MySqlConnectionStringResolver
{
    private const string PlaceholderMarker = "CHANGE_ME";

    public static string Resolve(IConfiguration configuration, string environmentName)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? configuration["MYSQLCONNSTR_DefaultConnection"]
            ?? configuration["AZURE_MYSQL_CONNECTIONSTRING"];

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
        }

        if (IsProductionLike(environmentName) &&
            connectionString.Contains(PlaceholderMarker, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("A real ConnectionStrings:DefaultConnection value is required outside development.");
        }

        var builder = new MySqlConnectionStringBuilder(connectionString);
        if (IsAzureMySqlHost(builder.Server) && RequiresTlsUpgrade(builder.SslMode))
        {
            builder.SslMode = MySqlSslMode.Required;
        }

        return builder.ConnectionString;
    }

    private static bool IsProductionLike(string? environmentName)
    {
        return !string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase) &&
               !string.Equals(environmentName, "Testing", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAzureMySqlHost(string? host)
    {
        return !string.IsNullOrWhiteSpace(host) &&
               host.EndsWith(".mysql.database.azure.com", StringComparison.OrdinalIgnoreCase);
    }

    private static bool RequiresTlsUpgrade(MySqlSslMode sslMode)
    {
        return sslMode is MySqlSslMode.None or MySqlSslMode.Preferred;
    }
}
