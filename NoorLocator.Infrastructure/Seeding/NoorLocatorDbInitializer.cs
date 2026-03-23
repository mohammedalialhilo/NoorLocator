using Microsoft.EntityFrameworkCore;
using NoorLocator.Domain.Entities;
using NoorLocator.Domain.Enums;
using NoorLocator.Infrastructure.Persistence;
using NoorLocator.Infrastructure.Security;

namespace NoorLocator.Infrastructure.Seeding;

public class NoorLocatorDbInitializer(NoorLocatorDbContext dbContext, PasswordHashingService passwordHashingService)
{
    public const string AdminEmail = "admin@noorlocator.local";
    public const string AdminPassword = "Admin123!Pass";
    public const string ManagerEmail = "manager@noorlocator.local";
    public const string ManagerPassword = "Manager123!Pass";
    public const string UserEmail = "user@noorlocator.local";
    public const string UserPassword = "User123!Pass";

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.Database.MigrateAsync(cancellationToken);

        await SeedLanguagesAsync(cancellationToken);
        await SeedUsersAsync(cancellationToken);
        await SeedCentersAndAssignmentsAsync(cancellationToken);
    }

    private async Task SeedLanguagesAsync(CancellationToken cancellationToken)
    {
        if (await dbContext.Languages.AnyAsync(cancellationToken))
        {
            return;
        }

        dbContext.Languages.AddRange(
            new Language { Name = "Arabic", Code = "ar" },
            new Language { Name = "Swedish", Code = "sv" },
            new Language { Name = "English", Code = "en" },
            new Language { Name = "Farsi", Code = "fa" },
            new Language { Name = "Urdu", Code = "ur" });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedUsersAsync(CancellationToken cancellationToken)
    {
        if (!await dbContext.Users.AnyAsync(user => user.Email == AdminEmail, cancellationToken))
        {
            dbContext.Users.Add(new User
            {
                Name = "NoorLocator Admin",
                Email = AdminEmail,
                PasswordHash = passwordHashingService.HashPassword(AdminPassword),
                Role = UserRole.Admin,
                CreatedAt = DateTime.UtcNow
            });
        }

        if (!await dbContext.Users.AnyAsync(user => user.Email == ManagerEmail, cancellationToken))
        {
            dbContext.Users.Add(new User
            {
                Name = "NoorLocator Manager",
                Email = ManagerEmail,
                PasswordHash = passwordHashingService.HashPassword(ManagerPassword),
                Role = UserRole.Manager,
                CreatedAt = DateTime.UtcNow
            });
        }

        if (!await dbContext.Users.AnyAsync(user => user.Email == UserEmail, cancellationToken))
        {
            dbContext.Users.Add(new User
            {
                Name = "NoorLocator User",
                Email = UserEmail,
                PasswordHash = passwordHashingService.HashPassword(UserPassword),
                Role = UserRole.User,
                CreatedAt = DateTime.UtcNow
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedCentersAndAssignmentsAsync(CancellationToken cancellationToken)
    {
        if (await dbContext.Centers.AnyAsync(cancellationToken))
        {
            return;
        }

        var languages = await dbContext.Languages.ToDictionaryAsync(language => language.Code, cancellationToken);
        var manager = await dbContext.Users.SingleAsync(user => user.Email == ManagerEmail, cancellationToken);

        var centers = new[]
        {
            new Center
            {
                Name = "Imam Ali Islamic Center",
                Address = "Vesterbrogade 120",
                City = "Copenhagen",
                Country = "Denmark",
                Latitude = 55.672100m,
                Longitude = 12.530300m,
                Description = "A seeded demo center for community discovery and majalis publishing.",
                CreatedAt = DateTime.UtcNow
            },
            new Center
            {
                Name = "Ahlulbayt Cultural Center",
                Address = "Sodermalm Demo 14",
                City = "Stockholm",
                Country = "Sweden",
                Latitude = 59.313500m,
                Longitude = 18.070800m,
                Description = "A seeded Stockholm center with multilingual majalis support.",
                CreatedAt = DateTime.UtcNow
            },
            new Center
            {
                Name = "Noor Community House",
                Address = "Mannerheimintie 22",
                City = "Helsinki",
                Country = "Finland",
                Latitude = 60.171900m,
                Longitude = 24.937500m,
                Description = "A seeded Helsinki center used for Phase 2 location and profile demos.",
                CreatedAt = DateTime.UtcNow
            }
        };

        dbContext.Centers.AddRange(centers);
        await dbContext.SaveChangesAsync(cancellationToken);

        dbContext.CenterLanguages.AddRange(
            new CenterLanguage { CenterId = centers[0].Id, LanguageId = languages["ar"].Id },
            new CenterLanguage { CenterId = centers[0].Id, LanguageId = languages["en"].Id },
            new CenterLanguage { CenterId = centers[1].Id, LanguageId = languages["sv"].Id },
            new CenterLanguage { CenterId = centers[1].Id, LanguageId = languages["en"].Id },
            new CenterLanguage { CenterId = centers[2].Id, LanguageId = languages["fa"].Id },
            new CenterLanguage { CenterId = centers[2].Id, LanguageId = languages["ur"].Id },
            new CenterLanguage { CenterId = centers[2].Id, LanguageId = languages["en"].Id });

        dbContext.CenterManagers.Add(new CenterManager
        {
            UserId = manager.Id,
            CenterId = centers[0].Id,
            Approved = true
        });

        var majlis = new Majlis
        {
            Title = "Weekly Thursday Majlis",
            Description = "Seeded majlis entry for the Phase 2 demo environment.",
            Date = DateTime.UtcNow.Date.AddDays(7).AddHours(19),
            Time = "19:00",
            CenterId = centers[0].Id,
            CreatedByManagerId = manager.Id,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Majalis.Add(majlis);
        await dbContext.SaveChangesAsync(cancellationToken);

        dbContext.MajlisLanguages.AddRange(
            new MajlisLanguage { MajlisId = majlis.Id, LanguageId = languages["ar"].Id },
            new MajlisLanguage { MajlisId = majlis.Id, LanguageId = languages["en"].Id });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
