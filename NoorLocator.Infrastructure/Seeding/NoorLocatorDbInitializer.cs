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
        var definitions = new[]
        {
            new Language { Name = "Arabic", Code = "ar" },
            new Language { Name = "Swedish", Code = "sv" },
            new Language { Name = "English", Code = "en" },
            new Language { Name = "Farsi", Code = "fa" },
            new Language { Name = "Urdu", Code = "ur" }
        };

        foreach (var definition in definitions)
        {
            if (await dbContext.Languages.AnyAsync(language => language.Code == definition.Code, cancellationToken))
            {
                continue;
            }

            dbContext.Languages.Add(definition);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedUsersAsync(CancellationToken cancellationToken)
    {
        await EnsureUserAsync("NoorLocator Admin", AdminEmail, AdminPassword, UserRole.Admin, cancellationToken);
        await EnsureUserAsync("NoorLocator Manager", ManagerEmail, ManagerPassword, UserRole.Manager, cancellationToken);
        await EnsureUserAsync("NoorLocator User", UserEmail, UserPassword, UserRole.User, cancellationToken);
    }

    private async Task SeedCentersAndAssignmentsAsync(CancellationToken cancellationToken)
    {
        var languages = await dbContext.Languages.ToDictionaryAsync(language => language.Code, cancellationToken);
        var manager = await dbContext.Users.SingleAsync(user => user.Email == ManagerEmail, cancellationToken);

        var centerDefinitions = new[]
        {
            new CenterSeed(
                "Imam Ali Islamic Center",
                "Vesterbrogade 120",
                "Copenhagen",
                "Denmark",
                55.672100m,
                12.530300m,
                "A flagship NoorLocator demo center in Copenhagen with multilingual public discovery data.",
                ["ar", "en", "ur"]),
            new CenterSeed(
                "Ahlulbayt Cultural Center",
                "Sodermalm Demo 14",
                "Stockholm",
                "Sweden",
                59.313500m,
                18.070800m,
                "A Stockholm center seeded for multilingual majalis discovery and manager workflows.",
                ["sv", "en", "ar"]),
            new CenterSeed(
                "Noor Community House",
                "Mannerheimintie 22",
                "Helsinki",
                "Finland",
                60.171900m,
                24.937500m,
                "A Helsinki center used for search, location, and center-language demos.",
                ["fa", "ur", "en"]),
            new CenterSeed(
                "Lady Zainab Majlis Hall",
                "Torggata 42",
                "Oslo",
                "Norway",
                59.913900m,
                10.752200m,
                "An Oslo gathering space seeded to make the public map and directory feel more complete.",
                ["ar", "en"]),
            new CenterSeed(
                "Aarhus Ahlulbayt Center",
                "Frederiks Alle 88",
                "Aarhus",
                "Denmark",
                56.149600m,
                10.203900m,
                "An Aarhus demo center that rounds out the Nordic public discovery experience.",
                ["ar", "en"])
        };

        var centers = new Dictionary<string, Center>(StringComparer.OrdinalIgnoreCase);
        foreach (var definition in centerDefinitions)
        {
            var center = await EnsureCenterAsync(definition, cancellationToken);
            centers[definition.Name] = center;

            foreach (var languageCode in definition.LanguageCodes)
            {
                await EnsureCenterLanguageAsync(center.Id, languages[languageCode].Id, cancellationToken);
            }
        }

        await EnsureCenterManagerAsync(manager.Id, centers["Imam Ali Islamic Center"].Id, cancellationToken);
        await EnsureCenterManagerAsync(manager.Id, centers["Ahlulbayt Cultural Center"].Id, cancellationToken);

        var majlisDefinitions = new[]
        {
            new MajlisSeed("Weekly Thursday Majlis", "A weekly seeded public majlis for nearby discovery and detail-page demos.", 3, "19:00", "Imam Ali Islamic Center", ["ar", "en"]),
            new MajlisSeed("Family Friday Gathering", "A family-oriented demo gathering with bilingual programming.", 10, "18:30", "Imam Ali Islamic Center", ["ar", "en", "ur"]),
            new MajlisSeed("Stockholm Reflection Night", "A public Stockholm majlis showcasing multilingual language badges.", 5, "19:30", "Ahlulbayt Cultural Center", ["sv", "en"]),
            new MajlisSeed("Helsinki Du'a And Discussion", "An upcoming Helsinki program seeded for the calendar and center detail views.", 8, "18:00", "Noor Community House", ["fa", "en"]),
            new MajlisSeed("Oslo Community Majlis", "A Nordic demo majlis with public center and map context.", 12, "19:00", "Lady Zainab Majlis Hall", ["ar", "en"]),
            new MajlisSeed("Aarhus Youth Circle", "A youth-focused majlis to enrich the published discovery feed.", 15, "17:30", "Aarhus Ahlulbayt Center", ["ar", "en"])
        };

        foreach (var definition in majlisDefinitions)
        {
            await EnsureMajlisAsync(definition, centers[definition.CenterName], manager.Id, languages, cancellationToken);
        }

        await SeedEventAnnouncementsAndImagesAsync(centers, manager.Id, cancellationToken);
    }

    private async Task EnsureUserAsync(string name, string email, string password, UserRole role, CancellationToken cancellationToken)
    {
        if (await dbContext.Users.AnyAsync(user => user.Email == email, cancellationToken))
        {
            return;
        }

        dbContext.Users.Add(new User
        {
            Name = name,
            Email = email,
            PasswordHash = passwordHashingService.HashPassword(password),
            Role = role,
            CreatedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Center> EnsureCenterAsync(CenterSeed definition, CancellationToken cancellationToken)
    {
        var existingCenter = await dbContext.Centers.SingleOrDefaultAsync(
            center => center.Name == definition.Name && center.City == definition.City && center.Country == definition.Country,
            cancellationToken);

        if (existingCenter is not null)
        {
            if (string.IsNullOrWhiteSpace(existingCenter.Description))
            {
                existingCenter.Description = definition.Description;
            }

            if (string.IsNullOrWhiteSpace(existingCenter.Address))
            {
                existingCenter.Address = definition.Address;
            }

            if (existingCenter.Latitude == default && existingCenter.Longitude == default)
            {
                existingCenter.Latitude = definition.Latitude;
                existingCenter.Longitude = definition.Longitude;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            return existingCenter;
        }

        var center = new Center
        {
            Name = definition.Name,
            Address = definition.Address,
            City = definition.City,
            Country = definition.Country,
            Latitude = definition.Latitude,
            Longitude = definition.Longitude,
            Description = definition.Description,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Centers.Add(center);
        await dbContext.SaveChangesAsync(cancellationToken);
        return center;
    }

    private async Task EnsureCenterLanguageAsync(int centerId, int languageId, CancellationToken cancellationToken)
    {
        if (await dbContext.CenterLanguages.AnyAsync(centerLanguage => centerLanguage.CenterId == centerId && centerLanguage.LanguageId == languageId, cancellationToken))
        {
            return;
        }

        dbContext.CenterLanguages.Add(new CenterLanguage
        {
            CenterId = centerId,
            LanguageId = languageId
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureCenterManagerAsync(int userId, int centerId, CancellationToken cancellationToken)
    {
        if (await dbContext.CenterManagers.AnyAsync(centerManager => centerManager.UserId == userId && centerManager.CenterId == centerId && centerManager.Approved, cancellationToken))
        {
            return;
        }

        dbContext.CenterManagers.Add(new CenterManager
        {
            UserId = userId,
            CenterId = centerId,
            Approved = true
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureMajlisAsync(
        MajlisSeed definition,
        Center center,
        int managerUserId,
        IReadOnlyDictionary<string, Language> languages,
        CancellationToken cancellationToken)
    {
        var targetDate = DateTime.UtcNow.Date.AddDays(definition.DaysFromToday);
        var existingMajlis = await dbContext.Majalis
            .Include(majlis => majlis.MajlisLanguages)
            .SingleOrDefaultAsync(
                majlis => majlis.CenterId == center.Id && majlis.Title == definition.Title,
                cancellationToken);

        if (existingMajlis is null)
        {
            existingMajlis = new Majlis
            {
                Title = definition.Title,
                Description = definition.Description,
                Date = targetDate,
                Time = definition.Time,
                CenterId = center.Id,
                CreatedByManagerId = managerUserId,
                CreatedAt = DateTime.UtcNow
            };

            dbContext.Majalis.Add(existingMajlis);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        else
        {
            existingMajlis.Description = definition.Description;
            existingMajlis.Date = targetDate;
            existingMajlis.Time = definition.Time;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var existingLanguageIds = existingMajlis.MajlisLanguages
            .Select(link => link.LanguageId)
            .ToHashSet();

        var missingLanguageLinks = definition.LanguageCodes
            .Select(code => languages[code].Id)
            .Where(languageId => !existingLanguageIds.Contains(languageId))
            .Select(languageId => new MajlisLanguage
            {
                MajlisId = existingMajlis.Id,
                LanguageId = languageId
            })
            .ToArray();

        if (missingLanguageLinks.Length > 0)
        {
            dbContext.MajlisLanguages.AddRange(missingLanguageLinks);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task SeedEventAnnouncementsAndImagesAsync(
        IReadOnlyDictionary<string, Center> centers,
        int managerUserId,
        CancellationToken cancellationToken)
    {
        const string placeholderImageUrl = "/assets/center-photo-placeholder.svg";

        await EnsureCenterImageAsync(centers["Imam Ali Islamic Center"].Id, placeholderImageUrl, managerUserId, isPrimary: true, cancellationToken);
        await EnsureCenterImageAsync(centers["Ahlulbayt Cultural Center"].Id, placeholderImageUrl, managerUserId, isPrimary: true, cancellationToken);

        await EnsureEventAnnouncementAsync(
            centers["Imam Ali Islamic Center"].Id,
            managerUserId,
            "Community Open House This Weekend",
            "Meet local volunteers, explore the center, and help families discover NoorLocator through a welcoming open house.",
            EventAnnouncementStatus.Published,
            placeholderImageUrl,
            cancellationToken);

        await EnsureEventAnnouncementAsync(
            centers["Ahlulbayt Cultural Center"].Id,
            managerUserId,
            "Ramadan Preparation Bulletin",
            "A draft manager announcement seeded to demonstrate direct publishing and status management in the manager workspace.",
            EventAnnouncementStatus.Draft,
            null,
            cancellationToken);
    }

    private async Task EnsureEventAnnouncementAsync(
        int centerId,
        int managerUserId,
        string title,
        string description,
        EventAnnouncementStatus status,
        string? imageUrl,
        CancellationToken cancellationToken)
    {
        var announcement = await dbContext.EventAnnouncements
            .SingleOrDefaultAsync(currentAnnouncement => currentAnnouncement.CenterId == centerId && currentAnnouncement.Title == title, cancellationToken);

        if (announcement is null)
        {
            dbContext.EventAnnouncements.Add(new EventAnnouncement
            {
                CenterId = centerId,
                CreatedByManagerId = managerUserId,
                Title = title,
                Description = description,
                Status = status,
                ImageUrl = imageUrl,
                CreatedAt = DateTime.UtcNow
            });
        }
        else
        {
            announcement.Description = description;
            announcement.Status = status;
            announcement.ImageUrl = imageUrl;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureCenterImageAsync(
        int centerId,
        string imageUrl,
        int managerUserId,
        bool isPrimary,
        CancellationToken cancellationToken)
    {
        var centerImage = await dbContext.CenterImages
            .SingleOrDefaultAsync(
                currentImage => currentImage.CenterId == centerId && currentImage.ImageUrl == imageUrl,
                cancellationToken);

        if (centerImage is null)
        {
            centerImage = new CenterImage
            {
                CenterId = centerId,
                ImageUrl = imageUrl,
                UploadedByManagerId = managerUserId,
                CreatedAt = DateTime.UtcNow,
                IsPrimary = isPrimary
            };

            dbContext.CenterImages.Add(centerImage);
        }
        else
        {
            centerImage.IsPrimary = isPrimary;
        }

        if (isPrimary)
        {
            var otherImages = await dbContext.CenterImages
                .Where(currentImage => currentImage.CenterId == centerId && currentImage.Id != centerImage.Id)
                .ToArrayAsync(cancellationToken);

            foreach (var otherImage in otherImages)
            {
                otherImage.IsPrimary = false;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private sealed record CenterSeed(
        string Name,
        string Address,
        string City,
        string Country,
        decimal Latitude,
        decimal Longitude,
        string Description,
        IReadOnlyCollection<string> LanguageCodes);

    private sealed record MajlisSeed(
        string Title,
        string Description,
        int DaysFromToday,
        string Time,
        string CenterName,
        IReadOnlyCollection<string> LanguageCodes);
}
