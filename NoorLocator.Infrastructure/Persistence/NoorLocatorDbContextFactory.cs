using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace NoorLocator.Infrastructure.Persistence;

public class NoorLocatorDbContextFactory : IDesignTimeDbContextFactory<NoorLocatorDbContext>
{
    public NoorLocatorDbContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();
        var apiPath = Path.Combine(basePath, "..", "NoorLocator.Api");
        var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? "Development";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.Exists(apiPath) ? apiPath : basePath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<NoorLocatorDbContext>();
        var connectionString = MySqlConnectionStringResolver.Resolve(configuration, environmentName);
        var serverVersion = MySqlServerVersionFactory.Create(configuration);

        optionsBuilder.UseMySql(
            connectionString,
            serverVersion,
            mysqlOptions =>
            {
                mysqlOptions.MigrationsAssembly(typeof(NoorLocatorDbContext).Assembly.FullName);
                mysqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null);
            });

        return new NoorLocatorDbContext(optionsBuilder.Options);
    }
}
