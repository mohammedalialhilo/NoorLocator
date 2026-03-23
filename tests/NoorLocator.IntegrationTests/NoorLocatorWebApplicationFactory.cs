using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NoorLocator.Domain.Entities;
using NoorLocator.Domain.Enums;
using NoorLocator.Infrastructure.Persistence;
using NoorLocator.Infrastructure.Security;

namespace NoorLocator.IntegrationTests;

public class NoorLocatorWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string databaseName = $"NoorLocatorTests-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<NoorLocatorDbContext>>();
            services.RemoveAll<NoorLocatorDbContext>();
            services.RemoveAll<IDbContextOptionsConfiguration<NoorLocatorDbContext>>();

            services.AddDbContext<NoorLocatorDbContext>(options =>
                options.UseInMemoryDatabase(databaseName));

            using var scope = services.BuildServiceProvider().CreateScope();
            var scopedServices = scope.ServiceProvider;
            var dbContext = scopedServices.GetRequiredService<NoorLocatorDbContext>();
            var passwordHashingService = scopedServices.GetRequiredService<PasswordHashingService>();

            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            SeedTestData(dbContext, passwordHashingService);
        });
    }

    private static void SeedTestData(NoorLocatorDbContext dbContext, PasswordHashingService passwordHashingService)
    {
        if (dbContext.Users.Any())
        {
            return;
        }

        var admin = new User
        {
            Id = 1,
            Name = "Integration Admin",
            Email = "admin@test.local",
            PasswordHash = passwordHashingService.HashPassword("Admin123!Pass"),
            Role = UserRole.Admin,
            CreatedAt = DateTime.UtcNow
        };
        var user = new User
        {
            Id = 2,
            Name = "Integration User",
            Email = "user@test.local",
            PasswordHash = passwordHashingService.HashPassword("User123!Pass"),
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow
        };
        var manager = new User
        {
            Id = 3,
            Name = "Integration Manager",
            Email = "manager@test.local",
            PasswordHash = passwordHashingService.HashPassword("Manager123!Pass"),
            Role = UserRole.Manager,
            CreatedAt = DateTime.UtcNow
        };

        var arabic = new Language { Id = 1, Name = "Arabic", Code = "ar" };
        var english = new Language { Id = 2, Name = "English", Code = "en" };
        var swedish = new Language { Id = 3, Name = "Swedish", Code = "sv" };

        var copenhagenCenter = new Center
        {
            Id = 1,
            Name = "Integration Copenhagen Center",
            Address = "Street 1",
            City = "Copenhagen",
            Country = "Denmark",
            Latitude = 55.6761m,
            Longitude = 12.5683m,
            Description = "Primary integration center.",
            CreatedAt = DateTime.UtcNow
        };
        var stockholmCenter = new Center
        {
            Id = 2,
            Name = "Integration Stockholm Center",
            Address = "Street 2",
            City = "Stockholm",
            Country = "Sweden",
            Latitude = 59.3293m,
            Longitude = 18.0686m,
            Description = "Secondary integration center.",
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Users.AddRange(admin, user, manager);
        dbContext.Languages.AddRange(arabic, english, swedish);
        dbContext.Centers.AddRange(copenhagenCenter, stockholmCenter);
        dbContext.CenterLanguages.AddRange(
            new CenterLanguage { CenterId = 1, LanguageId = 1, Center = copenhagenCenter, Language = arabic },
            new CenterLanguage { CenterId = 1, LanguageId = 2, Center = copenhagenCenter, Language = english },
            new CenterLanguage { CenterId = 2, LanguageId = 2, Center = stockholmCenter, Language = english },
            new CenterLanguage { CenterId = 2, LanguageId = 3, Center = stockholmCenter, Language = swedish });
        dbContext.CenterManagers.Add(new CenterManager
        {
            UserId = manager.Id,
            CenterId = copenhagenCenter.Id,
            Approved = true
        });
        dbContext.Majalis.Add(new Majlis
        {
            Id = 1,
            Title = "Integration Majlis",
            Description = "Public integration majlis.",
            Date = DateTime.UtcNow.Date.AddDays(3),
            Time = "19:00",
            CenterId = copenhagenCenter.Id,
            CreatedByManagerId = manager.Id,
            CreatedAt = DateTime.UtcNow
        });

        dbContext.SaveChanges();
    }
}
