using Microsoft.Extensions.DependencyInjection;
using NoorLocator.Application.Authentication.Dtos;
using NoorLocator.Application.Authentication.Validators;
using NoorLocator.Application.CenterImages.Dtos;
using NoorLocator.Application.CenterImages.Validators;
using NoorLocator.Application.Centers.Dtos;
using NoorLocator.Application.Centers.Validators;
using NoorLocator.Application.EventAnnouncements.Dtos;
using NoorLocator.Application.EventAnnouncements.Validators;
using NoorLocator.Application.Languages.Dtos;
using NoorLocator.Application.Languages.Validators;
using NoorLocator.Application.Management.Dtos;
using NoorLocator.Application.Management.Validators;
using NoorLocator.Application.Majalis.Dtos;
using NoorLocator.Application.Majalis.Validators;
using NoorLocator.Application.Profile.Dtos;
using NoorLocator.Application.Profile.Validators;
using NoorLocator.Application.Suggestions.Dtos;
using NoorLocator.Application.Suggestions.Validators;
using NoorLocator.Application.Validation;

namespace NoorLocator.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IValidator<RegisterRequestDto>, RegisterRequestValidator>();
        services.AddScoped<IValidator<LoginRequestDto>, LoginRequestValidator>();
        services.AddScoped<IValidator<CreateCenterRequestDto>, CreateCenterRequestValidator>();
        services.AddScoped<IValidator<UpdateCenterDto>, UpdateCenterValidator>();
        services.AddScoped<IValidator<CenterLocationQueryDto>, CenterLocationQueryValidator>();
        services.AddScoped<IValidator<NearestCentersQueryDto>, NearestCentersQueryValidator>();
        services.AddScoped<IValidator<CenterSearchQueryDto>, CenterSearchQueryValidator>();
        services.AddScoped<IValidator<CreateMajlisDto>, CreateMajlisValidator>();
        services.AddScoped<IValidator<UpdateMajlisDto>, UpdateMajlisValidator>();
        services.AddScoped<IValidator<CreateEventAnnouncementDto>, CreateEventAnnouncementValidator>();
        services.AddScoped<IValidator<UpdateEventAnnouncementDto>, UpdateEventAnnouncementValidator>();
        services.AddScoped<IValidator<UploadCenterImageDto>, UploadCenterImageValidator>();
        services.AddScoped<IValidator<UpdateProfileDto>, UpdateProfileValidator>();
        services.AddScoped<IValidator<CreateCenterLanguageSuggestionDto>, CreateCenterLanguageSuggestionValidator>();
        services.AddScoped<IValidator<ManagerRequestDto>, ManagerRequestValidator>();
        services.AddScoped<IValidator<CreateSuggestionDto>, CreateSuggestionValidator>();

        return services;
    }
}
