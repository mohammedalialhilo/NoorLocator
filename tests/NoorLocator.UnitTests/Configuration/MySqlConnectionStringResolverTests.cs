using Microsoft.Extensions.Configuration;
using NoorLocator.Infrastructure.Persistence;

namespace NoorLocator.UnitTests.Configuration;

public class MySqlConnectionStringResolverTests
{
    [Fact]
    public void Resolve_AddsRequiredSslMode_ForAzureMysqlHosts()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=noor-flex.mysql.database.azure.com;Port=3306;Database=Noorlocator;User=noor;Password=secret;"
            })
            .Build();

        var resolved = MySqlConnectionStringResolver.Resolve(configuration, "Production");

        Assert.Matches("(?i)Ssl\\s*Mode\\s*=\\s*Required", resolved);
    }

    [Fact]
    public void Resolve_RejectsPlaceholderConnectionString_OutsideDevelopment()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=127.0.0.1;Port=3306;Database=Noorlocator;User=root;Password=CHANGE_ME;"
            })
            .Build();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            MySqlConnectionStringResolver.Resolve(configuration, "Production"));

        Assert.Contains("required outside development", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
