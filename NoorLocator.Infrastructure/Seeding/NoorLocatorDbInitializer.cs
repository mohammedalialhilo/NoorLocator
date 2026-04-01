using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NoorLocator.Application.Common.Configuration;
using NoorLocator.Application.Common.Localization;
using NoorLocator.Domain.Entities;
using NoorLocator.Domain.Enums;
using NoorLocator.Infrastructure.Persistence;
using NoorLocator.Infrastructure.Security;

namespace NoorLocator.Infrastructure.Seeding;

public class NoorLocatorDbInitializer(
    NoorLocatorDbContext dbContext,
    PasswordHashingService passwordHashingService,
    IOptions<SeedingSettings> seedingOptions)
{
    private const string DemoManagerEmail = "manager@noorlocator.local";
    private const string DemoManagerPassword = "Manager123!Pass";
    private const string DemoUserEmail = "user@noorlocator.local";
    private const string DemoUserPassword = "User123!Pass";

    private readonly SeedingSettings seedingSettings = seedingOptions.Value;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var executionStrategy = dbContext.Database.CreateExecutionStrategy();
        await executionStrategy.ExecuteAsync(async () =>
        {
            if (seedingSettings.ApplyMigrations)
            {
                await dbContext.Database.MigrateAsync(cancellationToken);
            }

            if (seedingSettings.SeedReferenceData || seedingSettings.SeedDemoData)
            {
                await SeedLanguagesAsync(cancellationToken);
            }

            if (seedingSettings.SeedReferenceData)
            {
                await SeedAppContentAsync(cancellationToken);
            }

            if (seedingSettings.SeedAdminAccount)
            {
                ValidateAdminSeedConfiguration();
                await EnsureUserAsync(
                    seedingSettings.AdminName.Trim(),
                    seedingSettings.AdminEmail.Trim().ToLowerInvariant(),
                    seedingSettings.AdminPassword,
                    UserRole.Admin,
                    cancellationToken);
            }

            if (!seedingSettings.SeedDemoData)
            {
                return;
            }

            await SeedDemoUsersAsync(cancellationToken);
            await SeedCentersAndAssignmentsAsync(cancellationToken);
        });
    }

    private async Task SeedLanguagesAsync(CancellationToken cancellationToken)
    {
        var definitions = new[]
        {
            new Language { Name = "Arabic", Code = "ar" },
            new Language { Name = "Danish", Code = "da" },
            new Language { Name = "German", Code = "de" },
            new Language { Name = "Spanish", Code = "es" },
            new Language { Name = "Farsi", Code = "fa" },
            new Language { Name = "English", Code = "en" },
            new Language { Name = "Portuguese", Code = "pt" },
            new Language { Name = "Swedish", Code = "sv" },
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

    private async Task SeedDemoUsersAsync(CancellationToken cancellationToken)
    {
        await EnsureUserAsync("NoorLocator Manager", DemoManagerEmail, DemoManagerPassword, UserRole.Manager, cancellationToken);
        await EnsureUserAsync("NoorLocator User", DemoUserEmail, DemoUserPassword, UserRole.User, cancellationToken);
    }

    private async Task SeedAppContentAsync(CancellationToken cancellationToken)
    {
        var entries = new[]
        {
            new AppContentSeed("site.tagline", "Connecting you to Shia centers and majalis worldwide"),
            new AppContentSeed("site.attribution", "Driven by موكب خدام أهل البيت (عليهم السلام), Copenhagen, Denmark."),
            new AppContentSeed("home.hero.title", "Find Shia centers and majalis near you"),
            new AppContentSeed("home.hero.description", "NoorLocator helps followers of Ahlulbayt (AS) discover nearby centers, stay aware of majalis, and connect with their community through trusted, location-aware information."),
            new AppContentSeed("home.hero.highlight", "No follower of Ahlulbayt (AS) should feel disconnected from their community, no matter where they are in the world."),
            new AppContentSeed("home.mission.title", "A mission built around connection, trust, and service"),
            new AppContentSeed("home.mission.description", "NoorLocator connects people to the nearest Shia centers, enables centers to communicate their activities, and empowers community contributions with moderation that protects authenticity."),
            new AppContentSeed("home.mission.highlight", "This is not just a directory. It is a living, community-driven ecosystem for connection, knowledge, and service."),
            new AppContentSeed("home.features.title", "Key platform strengths"),
            new AppContentSeed("home.features.description", "The public experience is shaped by manifesto-driven priorities that keep NoorLocator clear, useful, and community-first."),
            new AppContentSeed("home.features.location.title", "Location-based discovery"),
            new AppContentSeed("home.features.location.description", "Surface the nearest centers and most relevant local community opportunities first."),
            new AppContentSeed("home.features.languages.title", "Multi-language majalis"),
            new AppContentSeed("home.features.languages.description", "Support diaspora communities through structured language data for centers and majalis."),
            new AppContentSeed("home.features.community.title", "Community-driven platform"),
            new AppContentSeed("home.features.community.description", "Enable community contributions while preserving trust through moderation and clearly defined roles."),
            new AppContentSeed("about.vision.title", "Vision"),
            new AppContentSeed("about.vision.description", "NoorLocator is built on a simple but powerful idea: no follower of Ahlulbayt (AS) should feel disconnected from their community, no matter where they are in the world."),
            new AppContentSeed("about.vision.highlight", "The platform focuses on global accessibility, location relevance, and stronger community connection across Europe and beyond."),
            new AppContentSeed("about.problem.title", "Problem statement"),
            new AppContentSeed("about.problem.description", "The manifesto identifies recurring challenges that NoorLocator exists to solve."),
            new AppContentSeed("about.problem.items.01", "Finding nearby Shia centers can be difficult when community spaces are scattered or not clearly visible online."),
            new AppContentSeed("about.problem.items.02", "People often lack timely awareness of majalis, gatherings, and center activities in their area."),
            new AppContentSeed("about.problem.items.03", "Language barriers make it harder for diaspora communities to access events and feel fully connected."),
            new AppContentSeed("about.mission.title", "Mission"),
            new AppContentSeed("about.mission.description", "NoorLocator exists to connect, inform, and empower people through trusted community infrastructure."),
            new AppContentSeed("about.mission.items.01", "Connect users to the nearest Shia centers."),
            new AppContentSeed("about.mission.items.02", "Enable centers to communicate their activities and public information clearly."),
            new AppContentSeed("about.mission.items.03", "Empower community contributions with moderation that protects authenticity and accuracy."),
            new AppContentSeed("about.principles.title", "Core principles"),
            new AppContentSeed("about.principles.description", "The product experience and system rules are intentionally shaped by the manifesto."),
            new AppContentSeed("about.principles.trust.title", "Trust and authenticity"),
            new AppContentSeed("about.principles.trust.description", "Public center data is moderated because religious community information must be accurate, verified, and safe to trust."),
            new AppContentSeed("about.principles.community.title", "Community-driven, admin-controlled"),
            new AppContentSeed("about.principles.community.description", "Users contribute, admins validate, and managers maintain center content so the platform can grow without misinformation."),
            new AppContentSeed("about.principles.language.title", "Multi-language accessibility"),
            new AppContentSeed("about.principles.language.description", "Structured language support reflects the diversity of diaspora communities and helps non-Arabic speakers find meaningful access."),
            new AppContentSeed("about.principles.location.title", "Location relevance"),
            new AppContentSeed("about.principles.location.description", "Nearest centers, accessible languages, and relevant events should appear where they matter most to the user."),
            new AppContentSeed("about.identity.title", "Who we are"),
            new AppContentSeed("about.identity.description", "NoorLocator is proudly initiated and driven by موكب خدام أهل البيت (عليهم السلام), Copenhagen, Denmark."),
            new AppContentSeed("about.identity.highlight", "The platform is designed as service to the community, not simply as software."),
            new AppContentSeed("about.closing.title", "Closing"),
            new AppContentSeed("about.closing.description", "NoorLocator is built with the intention of service so people can find remembrance, attend majalis, and stay connected to Ahlulbayt (AS)."),
            new AppContentSeed("about.closing.highlight", "Wherever someone lives in the world, NoorLocator aims to ensure they are never far from their community.")
        };

        foreach (var entry in entries)
        {
            await EnsureAppContentAsync(entry, cancellationToken);
        }
    }

    private async Task SeedCentersAndAssignmentsAsync(CancellationToken cancellationToken)
    {
        var languages = await dbContext.Languages.ToDictionaryAsync(language => language.Code, cancellationToken);
        var manager = await dbContext.Users.SingleAsync(user => user.Email == DemoManagerEmail, cancellationToken);

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
                ["ar", "da", "de", "en"]),
            new CenterSeed(
                "Ahlulbayt Cultural Center",
                "Sodermalm Demo 14",
                "Stockholm",
                "Sweden",
                59.313500m,
                18.070800m,
                "A Stockholm center seeded for multilingual majalis discovery and manager workflows.",
                ["ar", "en", "fa", "sv"]),
            new CenterSeed(
                "Noor Community House",
                "Mannerheimintie 22",
                "Helsinki",
                "Finland",
                60.171900m,
                24.937500m,
                "A Helsinki center used for search, location, and center-language demos.",
                ["en", "es", "fa", "pt"]),
            new CenterSeed(
                "Lady Zainab Majlis Hall",
                "Torggata 42",
                "Oslo",
                "Norway",
                59.913900m,
                10.752200m,
                "An Oslo gathering space seeded to make the public map and directory feel more complete.",
                ["ar", "de", "en", "pt"]),
            new CenterSeed(
                "Aarhus Ahlulbayt Center",
                "Frederiks Alle 88",
                "Aarhus",
                "Denmark",
                56.149600m,
                10.203900m,
                "An Aarhus demo center that rounds out the Nordic public discovery experience.",
                ["ar", "da", "en", "es"])
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
            PreferredLanguageCode = SupportedLanguageCatalog.FallbackLanguageCode,
            Role = role,
            CreatedAt = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            IsEmailVerified = true
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        var createdUser = await dbContext.Users.SingleAsync(user => user.Email == email, cancellationToken);
        if (!await dbContext.UserNotificationPreferences.AnyAsync(preference => preference.UserId == createdUser.Id, cancellationToken))
        {
            dbContext.UserNotificationPreferences.Add(new UserNotificationPreference
            {
                UserId = createdUser.Id,
                CreatedAtUtc = DateTime.UtcNow
            });

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private void ValidateAdminSeedConfiguration()
    {
        if (string.IsNullOrWhiteSpace(seedingSettings.AdminName) ||
            string.IsNullOrWhiteSpace(seedingSettings.AdminEmail) ||
            string.IsNullOrWhiteSpace(seedingSettings.AdminPassword))
        {
            throw new InvalidOperationException("Seeding:AdminName, Seeding:AdminEmail, and Seeding:AdminPassword are required when Seeding:SeedAdminAccount is enabled.");
        }

        if (seedingSettings.AdminPassword.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Seeding:AdminPassword must be a real value when Seeding:SeedAdminAccount is enabled.");
        }
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

    private async Task EnsureAppContentAsync(AppContentSeed definition, CancellationToken cancellationToken)
    {
        var appContent = await dbContext.AppContents
            .SingleOrDefaultAsync(
                currentContent => currentContent.Key == definition.Key && currentContent.LanguageCode == definition.LanguageCode,
                cancellationToken);

        if (appContent is null)
        {
            dbContext.AppContents.Add(new AppContent
            {
                Key = definition.Key,
                Value = definition.Value,
                LanguageCode = definition.LanguageCode
            });
        }
        else
        {
            appContent.Value = definition.Value;
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

    private sealed record AppContentSeed(
        string Key,
        string Value,
        string LanguageCode = "en");
}
