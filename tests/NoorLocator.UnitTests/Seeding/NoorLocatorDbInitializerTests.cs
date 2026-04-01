using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NoorLocator.Application.Common.Configuration;
using NoorLocator.Domain.Enums;
using NoorLocator.Infrastructure.Seeding;
using NoorLocator.Infrastructure.Security;
using NoorLocator.UnitTests.TestHelpers;

namespace NoorLocator.UnitTests.Seeding;

public class NoorLocatorDbInitializerTests
{
    [Fact]
    public async Task InitializeAsync_WithBootstrapSettings_SeedsReferenceDataAdminAndDemoUsers()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var passwordHashingService = new PasswordHashingService();
        var settings = Options.Create(new SeedingSettings
        {
            ApplyMigrations = false,
            SeedReferenceData = true,
            SeedAdminAccount = true,
            AdminName = "Bootstrap Admin",
            AdminEmail = "admin-bootstrap@test.local",
            AdminPassword = "BootstrapAdmin123!Pass",
            SeedDemoData = true
        });
        var initializer = new NoorLocatorDbInitializer(dbContext, passwordHashingService, settings);

        await initializer.InitializeAsync();

        var admin = await dbContext.Users.SingleAsync(user => user.Email == "admin-bootstrap@test.local");

        Assert.Equal(3, await dbContext.Users.CountAsync());
        Assert.Equal(9, await dbContext.Languages.CountAsync());
        Assert.Equal(5, await dbContext.Centers.CountAsync());
        Assert.Equal(6, await dbContext.Majalis.CountAsync());
        Assert.Equal(2, await dbContext.EventAnnouncements.CountAsync());
        Assert.Equal(2, await dbContext.CenterImages.CountAsync());
        Assert.True(await dbContext.Users.AnyAsync(user => user.Role == UserRole.Manager));
        Assert.True(await dbContext.Users.AnyAsync(user => user.Role == UserRole.User));
        Assert.True(passwordHashingService.VerifyPassword("BootstrapAdmin123!Pass", admin.PasswordHash));
    }

    [Fact]
    public async Task InitializeAsync_ThrowsWhenAdminBootstrapPasswordIsPlaceholder()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var initializer = new NoorLocatorDbInitializer(
            dbContext,
            new PasswordHashingService(),
            Options.Create(new SeedingSettings
            {
                ApplyMigrations = false,
                SeedReferenceData = true,
                SeedAdminAccount = true,
                AdminName = "Bootstrap Admin",
                AdminEmail = "admin-bootstrap@test.local",
                AdminPassword = "CHANGE_ME_NOW",
                SeedDemoData = false
            }));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => initializer.InitializeAsync());

        Assert.Contains("AdminPassword", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
