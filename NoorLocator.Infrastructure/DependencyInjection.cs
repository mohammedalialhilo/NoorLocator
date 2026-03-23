using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NoorLocator.Application.Authentication.Interfaces;
using NoorLocator.Application.Centers.Interfaces;
using NoorLocator.Application.Majalis.Interfaces;
using NoorLocator.Application.Management.Interfaces;
using NoorLocator.Application.Suggestions.Interfaces;
using NoorLocator.Infrastructure.Persistence;
using NoorLocator.Infrastructure.Services.Auth;
using NoorLocator.Infrastructure.Services.Centers;
using NoorLocator.Infrastructure.Services.Majalis;
using NoorLocator.Infrastructure.Services.Management;
using NoorLocator.Infrastructure.Services.Suggestions;

namespace NoorLocator.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<NoorLocatorDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sqlOptions => sqlOptions.MigrationsAssembly(typeof(NoorLocatorDbContext).Assembly.FullName)));

        services.AddScoped<IAuthService, PlaceholderAuthService>();
        services.AddScoped<ICenterService, PlaceholderCenterService>();
        services.AddScoped<ICenterRequestService, PlaceholderCenterRequestService>();
        services.AddScoped<IMajlisService, PlaceholderMajlisService>();
        services.AddScoped<IManagerService, PlaceholderManagerService>();
        services.AddScoped<ISuggestionService, PlaceholderSuggestionService>();

        return services;
    }
}
