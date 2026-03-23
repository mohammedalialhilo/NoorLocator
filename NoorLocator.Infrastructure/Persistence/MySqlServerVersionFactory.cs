using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace NoorLocator.Infrastructure.Persistence;

public static class MySqlServerVersionFactory
{
    private const string DefaultServerVersion = "8.0.36";

    public static MySqlServerVersion Create(IConfiguration configuration)
    {
        var configuredVersion = configuration["MySql:ServerVersion"];
        var version = Version.Parse(string.IsNullOrWhiteSpace(configuredVersion) ? DefaultServerVersion : configuredVersion);
        return new MySqlServerVersion(version);
    }
}
