using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NoorLocator.Infrastructure.Persistence;
using NoorLocator.Infrastructure.Services.Audit;

namespace NoorLocator.UnitTests.TestHelpers;

internal static class TestDbContextFactory
{
    public static NoorLocatorDbContext CreateContext(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<NoorLocatorDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString("N"))
            .Options;

        return new NoorLocatorDbContext(options);
    }

    public static AuditLogger CreateAuditLogger(NoorLocatorDbContext dbContext)
    {
        return new AuditLogger(
            dbContext,
            new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext()
            });
    }
}
