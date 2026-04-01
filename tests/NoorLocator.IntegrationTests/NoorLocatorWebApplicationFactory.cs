using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NoorLocator.Application.Common.Localization;
using NoorLocator.Domain.Entities;
using NoorLocator.Domain.Enums;
using NoorLocator.Infrastructure.Persistence;
using NoorLocator.Infrastructure.Security;

namespace NoorLocator.IntegrationTests;

public class NoorLocatorWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string databaseName = $"NoorLocatorTests-{Guid.NewGuid():N}";
    private readonly string uploadRootPath = Path.Combine(".codex-temp", "integration-uploads", Guid.NewGuid().ToString("N"));
    private string? resolvedUploadRootPath;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, configurationBuilder) =>
        {
            resolvedUploadRootPath = Path.GetFullPath(Path.Combine(context.HostingEnvironment.ContentRootPath, uploadRootPath));
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MediaStorage:RelativeRootPath"] = uploadRootPath,
                ["SmtpSettings:Host"] = "",
                ["SmtpSettings:Username"] = "",
                ["SmtpSettings:Password"] = "",
                ["SmtpSettings:WriteToPickupDirectoryWhenDisabled"] = "true",
                ["SmtpSettings:PickupDirectory"] = ".codex-temp/test-email-outbox"
            });
        });

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
            PreferredLanguageCode = SupportedLanguageCatalog.FallbackLanguageCode,
            Role = UserRole.Admin,
            CreatedAt = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            IsEmailVerified = true
        };
        var user = new User
        {
            Id = 2,
            Name = "Integration User",
            Email = "user@test.local",
            PasswordHash = passwordHashingService.HashPassword("User123!Pass"),
            PreferredLanguageCode = SupportedLanguageCatalog.FallbackLanguageCode,
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            IsEmailVerified = true
        };
        var manager = new User
        {
            Id = 3,
            Name = "Integration Manager",
            Email = "manager@test.local",
            PasswordHash = passwordHashingService.HashPassword("Manager123!Pass"),
            PreferredLanguageCode = SupportedLanguageCatalog.FallbackLanguageCode,
            Role = UserRole.Manager,
            CreatedAt = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            IsEmailVerified = true
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
        dbContext.UserNotificationPreferences.AddRange(
            new UserNotificationPreference { UserId = admin.Id, CreatedAtUtc = DateTime.UtcNow },
            new UserNotificationPreference { UserId = user.Id, CreatedAtUtc = DateTime.UtcNow },
            new UserNotificationPreference { UserId = manager.Id, CreatedAtUtc = DateTime.UtcNow });
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
        dbContext.AppContents.AddRange(
            new AppContent { Id = 1, Key = "site.tagline", Value = "Connecting you to Shia centers and majalis worldwide", LanguageCode = "en" },
            new AppContent { Id = 2, Key = "site.attribution", Value = "Driven by موكب خدام أهل البيت (عليهم السلام), Copenhagen, Denmark.", LanguageCode = "en" },
            new AppContent { Id = 3, Key = "home.hero.title", Value = "Find Shia centers and majalis near you", LanguageCode = "en" },
            new AppContent { Id = 4, Key = "home.hero.description", Value = "NoorLocator helps followers of Ahlulbayt (AS) discover nearby centers, stay aware of majalis, and connect with their community through trusted, location-aware information.", LanguageCode = "en" },
            new AppContent { Id = 5, Key = "home.hero.highlight", Value = "No follower of Ahlulbayt (AS) should feel disconnected from their community, no matter where they are in the world.", LanguageCode = "en" },
            new AppContent { Id = 6, Key = "home.mission.title", Value = "A mission built around connection, trust, and service", LanguageCode = "en" },
            new AppContent { Id = 7, Key = "home.mission.description", Value = "NoorLocator connects people to the nearest Shia centers, enables centers to communicate their activities, and empowers community contributions with moderation that protects authenticity.", LanguageCode = "en" },
            new AppContent { Id = 8, Key = "home.mission.highlight", Value = "This is not just a directory. It is a living, community-driven ecosystem for connection, knowledge, and service.", LanguageCode = "en" },
            new AppContent { Id = 9, Key = "home.features.title", Value = "Key platform strengths", LanguageCode = "en" },
            new AppContent { Id = 10, Key = "home.features.description", Value = "The public experience is shaped by manifesto-driven priorities that keep NoorLocator clear, useful, and community-first.", LanguageCode = "en" },
            new AppContent { Id = 11, Key = "home.features.location.title", Value = "Location-based discovery", LanguageCode = "en" },
            new AppContent { Id = 12, Key = "home.features.location.description", Value = "Surface the nearest centers and most relevant local community opportunities first.", LanguageCode = "en" },
            new AppContent { Id = 13, Key = "home.features.languages.title", Value = "Multi-language majalis", LanguageCode = "en" },
            new AppContent { Id = 14, Key = "home.features.languages.description", Value = "Support diaspora communities through structured language data for centers and majalis.", LanguageCode = "en" },
            new AppContent { Id = 15, Key = "home.features.community.title", Value = "Community-driven platform", LanguageCode = "en" },
            new AppContent { Id = 16, Key = "home.features.community.description", Value = "Enable community contributions while preserving trust through moderation and clearly defined roles.", LanguageCode = "en" },
            new AppContent { Id = 17, Key = "about.vision.title", Value = "Vision", LanguageCode = "en" },
            new AppContent { Id = 18, Key = "about.vision.description", Value = "NoorLocator is built on a simple but powerful idea: no follower of Ahlulbayt (AS) should feel disconnected from their community, no matter where they are in the world.", LanguageCode = "en" },
            new AppContent { Id = 19, Key = "about.vision.highlight", Value = "The platform focuses on global accessibility, location relevance, and stronger community connection across Europe and beyond.", LanguageCode = "en" },
            new AppContent { Id = 20, Key = "about.problem.title", Value = "Problem statement", LanguageCode = "en" },
            new AppContent { Id = 21, Key = "about.problem.description", Value = "The manifesto identifies recurring challenges that NoorLocator exists to solve.", LanguageCode = "en" },
            new AppContent { Id = 22, Key = "about.problem.items.01", Value = "Finding nearby Shia centers can be difficult when community spaces are scattered or not clearly visible online.", LanguageCode = "en" },
            new AppContent { Id = 23, Key = "about.problem.items.02", Value = "People often lack timely awareness of majalis, gatherings, and center activities in their area.", LanguageCode = "en" },
            new AppContent { Id = 24, Key = "about.problem.items.03", Value = "Language barriers make it harder for diaspora communities to access events and feel fully connected.", LanguageCode = "en" },
            new AppContent { Id = 25, Key = "about.mission.title", Value = "Mission", LanguageCode = "en" },
            new AppContent { Id = 26, Key = "about.mission.description", Value = "NoorLocator exists to connect, inform, and empower people through trusted community infrastructure.", LanguageCode = "en" },
            new AppContent { Id = 27, Key = "about.mission.items.01", Value = "Connect users to the nearest Shia centers.", LanguageCode = "en" },
            new AppContent { Id = 28, Key = "about.mission.items.02", Value = "Enable centers to communicate their activities and public information clearly.", LanguageCode = "en" },
            new AppContent { Id = 29, Key = "about.mission.items.03", Value = "Empower community contributions with moderation that protects authenticity and accuracy.", LanguageCode = "en" },
            new AppContent { Id = 30, Key = "about.principles.title", Value = "Core principles", LanguageCode = "en" },
            new AppContent { Id = 31, Key = "about.principles.description", Value = "The product experience and system rules are intentionally shaped by the manifesto.", LanguageCode = "en" },
            new AppContent { Id = 32, Key = "about.principles.trust.title", Value = "Trust and authenticity", LanguageCode = "en" },
            new AppContent { Id = 33, Key = "about.principles.trust.description", Value = "Public center data is moderated because religious community information must be accurate, verified, and safe to trust.", LanguageCode = "en" },
            new AppContent { Id = 34, Key = "about.principles.community.title", Value = "Community-driven, admin-controlled", LanguageCode = "en" },
            new AppContent { Id = 35, Key = "about.principles.community.description", Value = "Users contribute, admins validate, and managers maintain center content so the platform can grow without misinformation.", LanguageCode = "en" },
            new AppContent { Id = 36, Key = "about.principles.language.title", Value = "Multi-language accessibility", LanguageCode = "en" },
            new AppContent { Id = 37, Key = "about.principles.language.description", Value = "Structured language support reflects the diversity of diaspora communities and helps non-Arabic speakers find meaningful access.", LanguageCode = "en" },
            new AppContent { Id = 38, Key = "about.principles.location.title", Value = "Location relevance", LanguageCode = "en" },
            new AppContent { Id = 39, Key = "about.principles.location.description", Value = "Nearest centers, accessible languages, and relevant events should appear where they matter most to the user.", LanguageCode = "en" },
            new AppContent { Id = 40, Key = "about.identity.title", Value = "Who we are", LanguageCode = "en" },
            new AppContent { Id = 41, Key = "about.identity.description", Value = "NoorLocator is proudly initiated and driven by موكب خدام أهل البيت (عليهم السلام), Copenhagen, Denmark.", LanguageCode = "en" },
            new AppContent { Id = 42, Key = "about.identity.highlight", Value = "The platform is designed as service to the community, not simply as software.", LanguageCode = "en" },
            new AppContent { Id = 43, Key = "about.closing.title", Value = "Closing", LanguageCode = "en" },
            new AppContent { Id = 44, Key = "about.closing.description", Value = "NoorLocator is built with the intention of service so people can find remembrance, attend majalis, and stay connected to Ahlulbayt (AS).", LanguageCode = "en" },
            new AppContent { Id = 45, Key = "about.closing.highlight", Value = "Wherever someone lives in the world, NoorLocator aims to ensure they are never far from their community.", LanguageCode = "en" });

        dbContext.SaveChanges();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
        {
            return;
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(resolvedUploadRootPath) && Directory.Exists(resolvedUploadRootPath))
            {
                Directory.Delete(resolvedUploadRootPath, recursive: true);
            }
        }
        catch
        {
            // Ignore upload cleanup failures in test teardown.
        }
    }

    internal string ResolveStoredUploadPath(string publicUrl)
    {
        if (string.IsNullOrWhiteSpace(resolvedUploadRootPath))
        {
            throw new InvalidOperationException("The integration upload root path has not been resolved.");
        }

        const string publicBasePath = "/uploads/";
        if (!publicUrl.StartsWith(publicBasePath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"The public URL '{publicUrl}' does not use the expected local uploads base path.");
        }

        var relativePath = publicUrl[publicBasePath.Length..].Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(resolvedUploadRootPath, relativePath);
    }
}
