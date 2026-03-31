using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NoorLocator.Application.Admin.Interfaces;
using NoorLocator.Application.Authentication.Interfaces;
using NoorLocator.Application.CenterImages.Interfaces;
using NoorLocator.Application.Common.Configuration;
using NoorLocator.Application.Content.Interfaces;
using NoorLocator.Application.Centers.Interfaces;
using NoorLocator.Application.EventAnnouncements.Interfaces;
using NoorLocator.Application.Languages.Interfaces;
using NoorLocator.Application.Notifications.Interfaces;
using NoorLocator.Application.Majalis.Interfaces;
using NoorLocator.Application.Management.Interfaces;
using NoorLocator.Application.Profile.Interfaces;
using NoorLocator.Application.Suggestions.Interfaces;
using NoorLocator.Infrastructure.Persistence;
using NoorLocator.Infrastructure.Seeding;
using NoorLocator.Infrastructure.Security;
using NoorLocator.Infrastructure.Services.Admin;
using NoorLocator.Infrastructure.Services.Auth;
using NoorLocator.Infrastructure.Services.Audit;
using NoorLocator.Infrastructure.Services.Centers;
using NoorLocator.Infrastructure.Services.CenterImages;
using NoorLocator.Infrastructure.Services.Content;
using NoorLocator.Infrastructure.Services.EventAnnouncements;
using NoorLocator.Infrastructure.Services.Majalis;
using NoorLocator.Infrastructure.Services.Media;
using NoorLocator.Infrastructure.Services.Management;
using NoorLocator.Infrastructure.Services.Notifications;
using NoorLocator.Infrastructure.Services.Profile;
using NoorLocator.Infrastructure.Services.Suggestions;
using NoorLocator.Infrastructure.Services.Languages;
using NoorLocator.Infrastructure.Services.Email;

namespace NoorLocator.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.Configure<FrontendSettings>(configuration.GetSection(FrontendSettings.SectionName));
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<AuthFlowSettings>(configuration.GetSection(AuthFlowSettings.SectionName));
        services.Configure<MediaStorageSettings>(configuration.GetSection(MediaStorageSettings.SectionName));
        services.Configure<SmtpSettings>(configuration.GetSection(SmtpSettings.SectionName));
        services.Configure<AzureBlobStorageSettings>(configuration.GetSection(AzureBlobStorageSettings.SectionName));
        services.PostConfigure<AzureBlobStorageSettings>(settings =>
        {
            if (string.IsNullOrWhiteSpace(settings.ConnectionString))
            {
                settings.ConnectionString = configuration.GetConnectionString(AzureBlobStorageSettings.SectionName)
                    ?? configuration["AZURE_STORAGE_CONNECTION_STRING"]
                    ?? string.Empty;
            }
        });
        services.Configure<SeedingSettings>(configuration.GetSection(SeedingSettings.SectionName));
        services.AddHttpContextAccessor();

        var connectionString = ResolveConnectionString(configuration, environment);
        var serverVersion = MySqlServerVersionFactory.Create(configuration);

        services.AddDbContext<NoorLocatorDbContext>(options =>
            options.UseMySql(
                connectionString,
                serverVersion,
                mysqlOptions =>
                {
                    mysqlOptions.MigrationsAssembly(typeof(NoorLocatorDbContext).Assembly.FullName);
                    mysqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorNumbersToAdd: null);
                }));

        services.AddScoped<PasswordHashingService>();
        services.AddScoped<JwtTokenFactory>();
        services.AddScoped<AuditLogger>();
        services.AddSingleton<EmailDispatchRecorder>();
        services.AddScoped<IEmailDeliveryService, EmailDeliveryService>();
        services.AddScoped<INoorLocatorEmailService, NoorLocatorEmailService>();
        services.AddScoped<LocalMediaStorageService>();
        services.AddScoped<AzureBlobStorageService>();
        services.AddScoped<IMediaStorageService>(serviceProvider =>
        {
            var mediaStorageSettings = serviceProvider.GetRequiredService<IOptions<MediaStorageSettings>>().Value;
            return ResolveMediaStorageService(serviceProvider, mediaStorageSettings);
        });
        services.AddScoped<NoorLocatorDbInitializer>();

        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IAppContentService, AppContentService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICenterImageService, CenterImageService>();
        services.AddScoped<ICenterService, CenterService>();
        services.AddScoped<IUserCenterEngagementService, UserCenterEngagementService>();
        services.AddScoped<ICenterRequestService, CenterRequestService>();
        services.AddScoped<IEventAnnouncementService, EventAnnouncementService>();
        services.AddScoped<ILanguageService, LanguageService>();
        services.AddScoped<IMajlisService, MajlisService>();
        services.AddScoped<IManagerCenterAccessService, ManagerCenterAccessService>();
        services.AddScoped<IManagerService, ManagerService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<ISuggestionService, SuggestionService>();

        return services;
    }

    private static string ResolveConnectionString(IConfiguration configuration, IHostEnvironment environment)
    {
        return MySqlConnectionStringResolver.Resolve(configuration, environment.EnvironmentName);
    }

    private static IMediaStorageService ResolveMediaStorageService(IServiceProvider serviceProvider, MediaStorageSettings settings)
    {
        var provider = string.IsNullOrWhiteSpace(settings.Provider)
            ? MediaStorageProviders.Local
            : settings.Provider.Trim();

        if (provider.Equals(MediaStorageProviders.AzureBlob, StringComparison.OrdinalIgnoreCase))
        {
            ValidateAzureBlobStorageSettings(serviceProvider.GetRequiredService<IOptions<AzureBlobStorageSettings>>().Value);
            return serviceProvider.GetRequiredService<AzureBlobStorageService>();
        }

        if (!provider.Equals(MediaStorageProviders.Local, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Unsupported media storage provider '{provider}'.");
        }

        return serviceProvider.GetRequiredService<LocalMediaStorageService>();
    }

    private static void ValidateAzureBlobStorageSettings(AzureBlobStorageSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.ContainerName))
        {
            throw new InvalidOperationException("AzureBlobStorage:ContainerName must be configured when MediaStorage:Provider is AzureBlob.");
        }

        if (!string.IsNullOrWhiteSpace(settings.PublicBaseUrl) &&
            !Uri.TryCreate(settings.PublicBaseUrl, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException("AzureBlobStorage:PublicBaseUrl must be an absolute URI when configured.");
        }

        var hasConnectionString = !string.IsNullOrWhiteSpace(settings.ConnectionString);
        var hasServiceUri = !string.IsNullOrWhiteSpace(settings.ServiceUri);
        var hasAccountName = !string.IsNullOrWhiteSpace(settings.AccountName);
        if (!hasConnectionString && !hasServiceUri && !hasAccountName)
        {
            throw new InvalidOperationException("AzureBlobStorage requires a ConnectionString, ServiceUri, or AccountName when MediaStorage:Provider is AzureBlob.");
        }
    }
}
