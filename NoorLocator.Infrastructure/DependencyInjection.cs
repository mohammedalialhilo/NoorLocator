using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NoorLocator.Application.Admin.Interfaces;
using NoorLocator.Application.Authentication.Interfaces;
using NoorLocator.Application.CenterImages.Interfaces;
using NoorLocator.Application.Common.Configuration;
using NoorLocator.Application.Content.Interfaces;
using NoorLocator.Application.Centers.Interfaces;
using NoorLocator.Application.EventAnnouncements.Interfaces;
using NoorLocator.Application.Languages.Interfaces;
using NoorLocator.Application.Majalis.Interfaces;
using NoorLocator.Application.Management.Interfaces;
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
using NoorLocator.Infrastructure.Services.Suggestions;
using NoorLocator.Infrastructure.Services.Languages;

namespace NoorLocator.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<MediaStorageSettings>(configuration.GetSection(MediaStorageSettings.SectionName));
        services.AddHttpContextAccessor();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
        var serverVersion = MySqlServerVersionFactory.Create(configuration);

        services.AddDbContext<NoorLocatorDbContext>(options =>
            options.UseMySql(
                connectionString,
                serverVersion,
                mysqlOptions => mysqlOptions.MigrationsAssembly(typeof(NoorLocatorDbContext).Assembly.FullName)));

        services.AddScoped<PasswordHashingService>();
        services.AddScoped<JwtTokenFactory>();
        services.AddScoped<AuditLogger>();
        services.AddScoped<IMediaStorageService, LocalMediaStorageService>();
        services.AddScoped<NoorLocatorDbInitializer>();

        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IAppContentService, AppContentService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICenterImageService, CenterImageService>();
        services.AddScoped<ICenterService, CenterService>();
        services.AddScoped<ICenterRequestService, CenterRequestService>();
        services.AddScoped<IEventAnnouncementService, EventAnnouncementService>();
        services.AddScoped<ILanguageService, LanguageService>();
        services.AddScoped<IMajlisService, MajlisService>();
        services.AddScoped<IManagerCenterAccessService, ManagerCenterAccessService>();
        services.AddScoped<IManagerService, ManagerService>();
        services.AddScoped<ISuggestionService, SuggestionService>();

        return services;
    }
}
