using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Microsoft.Extensions.Options;
using NoorLocator.Api.Extensions;
using NoorLocator.Api.Middleware;
using NoorLocator.Api.OpenApi;
using NoorLocator.Application;
using NoorLocator.Application.Common.Configuration;
using NoorLocator.Application.Common.Models;
using NoorLocator.Infrastructure;
using NoorLocator.Infrastructure.Persistence;
using NoorLocator.Infrastructure.Seeding;
using NoorLocator.Infrastructure.Services.Media;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState.Values
            .SelectMany(value => value.Errors)
            .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage) ? "Validation failed." : error.ErrorMessage)
            .Distinct()
            .ToArray();

        return new BadRequestObjectResult(ApiResponse<ApiErrorDetails>.Failure(
            errors.FirstOrDefault() ?? "Validation failed.",
            new ApiErrorDetails
            {
                TraceId = context.HttpContext.TraceIdentifier,
                Errors = errors
            }));
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();
var jwtKey = ResolveJwtKey(builder.Environment, jwtSettings.Key);
var configuredFrontendSettings = builder.Configuration.GetSection(FrontendSettings.SectionName).Get<FrontendSettings>() ?? new FrontendSettings();
var mediaStorageSettings = builder.Configuration.GetSection(MediaStorageSettings.SectionName).Get<MediaStorageSettings>() ?? new MediaStorageSettings();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment() && !builder.Environment.IsEnvironment("Testing");
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var principal = context.Principal;
                var userId = principal?.TryGetUserId();
                var sessionId = principal?.TryGetSessionId();

                if (!userId.HasValue || string.IsNullOrWhiteSpace(sessionId))
                {
                    context.Fail("The authenticated session is invalid.");
                    return;
                }

                var dbContext = context.HttpContext.RequestServices.GetRequiredService<NoorLocatorDbContext>();
                var sessionIsActive = await dbContext.RefreshTokens
                    .AsNoTracking()
                    .AnyAsync(
                        refreshToken =>
                            refreshToken.UserId == userId.Value &&
                            refreshToken.SessionId == sessionId &&
                            refreshToken.RevokedAtUtc == null &&
                            refreshToken.ExpiresAtUtc > DateTime.UtcNow,
                        context.HttpContext.RequestAborted);

                if (!sessionIsActive)
                {
                    context.Fail("The authenticated session is no longer active.");
                }
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminArea", policy =>
        policy.RequireRole("Admin"));

    options.AddPolicy("ManagerArea", policy =>
        policy.RequireRole("Manager", "Admin"));
});

var allowedOrigins = (builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>())
    .Where(origin => Uri.TryCreate(origin?.Trim(), UriKind.Absolute, out _))
    .Select(origin => origin.Trim().TrimEnd('/'))
    .Concat(GetAdditionalAllowedOrigins(configuredFrontendSettings))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();
var allowOpenCors = builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing");

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        if (allowedOrigins.Length == 0)
        {
            if (!allowOpenCors)
            {
                throw new InvalidOperationException("Cors:AllowedOrigins must be configured outside development and testing environments.");
            }

            policy.AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
        else
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "NoorLocator API",
        Version = "v1",
        Description = "Moderated center discovery, contributions, manager workflows, and admin governance for NoorLocator.",
        Contact = new OpenApiContact
        {
            Name = "NoorLocator"
        }
    });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter a bearer token in the format: Bearer {your JWT}",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = JwtBearerDefaults.AuthenticationScheme,
        BearerFormat = "JWT"
    };

    options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, securityScheme);
    options.OperationFilter<SwaggerDefaultResponsesOperationFilter>();
    options.SupportNonNullableReferenceTypes();
    options.CustomSchemaIds(SwaggerSchemaIdFormatter.Format);

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }
});

var app = builder.Build();
var frontendSettings = app.Services.GetRequiredService<IOptions<FrontendSettings>>().Value;

if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var initializer = scope.ServiceProvider.GetRequiredService<NoorLocatorDbInitializer>();
    await initializer.InitializeAsync();
}

app.UseMiddleware<ApiExceptionMiddleware>();
app.UseResponseCompression();
if (app.Configuration.GetValue<bool>("ReverseProxy:UseForwardedHeaders"))
{
    app.UseForwardedHeaders();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.Use(async (context, next) =>
{
    if (!context.Request.Path.Equals("/js/runtime-config.js", StringComparison.OrdinalIgnoreCase))
    {
        await next();
        return;
    }

    context.Response.ContentType = "application/javascript; charset=utf-8";
    context.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
    context.Response.Headers.Pragma = "no-cache";
    context.Response.Headers.Expires = "0";

    var payload = JsonSerializer.Serialize(new
    {
        apiBaseUrl = NormalizeFrontendApiBaseUrl(frontendSettings.ApiBaseUrl)
    });

    await context.Response.WriteAsync($"window.NoorLocatorRuntimeConfig = {payload};");
});

if (mediaStorageSettings.Provider.Equals(MediaStorageProviders.Local, StringComparison.OrdinalIgnoreCase))
{
    var uploadsRootPath = MediaStoragePathResolver.ResolveStorageRootPath(app.Environment, mediaStorageSettings);
    var uploadsRequestPath = MediaStoragePathResolver.NormalizePublicBasePath(mediaStorageSettings.PublicBasePath);
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(uploadsRootPath),
        RequestPath = uploadsRequestPath
    });
}

var configuredFrontendPath = frontendSettings.RelativeRootPath;
var frontendCandidates = new[]
{
    configuredFrontendPath,
    "..\\frontend",
    "frontend"
}
.Where(path => !string.IsNullOrWhiteSpace(path))
.Select(path => Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, path!)))
.Distinct()
.ToArray();

var frontendPath = frontendCandidates.FirstOrDefault(Directory.Exists);
if (!string.IsNullOrWhiteSpace(frontendPath))
{
    var frontendFileProvider = new PhysicalFileProvider(frontendPath);
    var aboutPagePath = Path.Combine(frontendPath, "about.html");
    var nonCacheableFrontendPages = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "/dashboard.html",
        "/profile.html",
        "/manager.html",
        "/admin.html",
        "/logout.html"
    };

    app.UseDefaultFiles(new DefaultFilesOptions
    {
        FileProvider = frontendFileProvider
    });

    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = frontendFileProvider,
        OnPrepareResponse = context =>
        {
            var requestPath = context.Context.Request.Path.Value ?? string.Empty;
            if (!nonCacheableFrontendPages.Contains(requestPath))
            {
                return;
            }

            context.Context.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
            context.Context.Response.Headers.Pragma = "no-cache";
            context.Context.Response.Headers.Expires = "0";
        }
    });

    if (File.Exists(aboutPagePath))
    {
        app.MapGet("/about", async context =>
        {
            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.SendFileAsync(aboutPagePath);
        });
    }
}
else
{
    app.Logger.LogWarning("Frontend static root was not found. Configure Frontend:RelativeRootPath so the NoorLocator frontend can be served.");
}

var swaggerEnabled = app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled");
if (swaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (app.Configuration.GetValue("Https:RedirectionEnabled", app.Environment.IsDevelopment()))
{
    app.UseHttpsRedirection();
}

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

static IEnumerable<string> GetAdditionalAllowedOrigins(FrontendSettings frontendSettings)
{
    if (!Uri.TryCreate(frontendSettings.PublicOrigin?.Trim(), UriKind.Absolute, out var publicOrigin))
    {
        return Array.Empty<string>();
    }

    return [publicOrigin.AbsoluteUri.TrimEnd('/')];
}

static string NormalizeFrontendApiBaseUrl(string? configuredApiBaseUrl)
{
    return string.IsNullOrWhiteSpace(configuredApiBaseUrl)
        ? string.Empty
        : configuredApiBaseUrl.Trim().TrimEnd('/');
}

static string ResolveJwtKey(IHostEnvironment environment, string? configuredKey)
{
    const string placeholder = "CHANGE-ME-TO-A-SECURE-32-CHARACTER-MINIMUM-SECRET";

    var key = string.IsNullOrWhiteSpace(configuredKey)
        ? placeholder
        : configuredKey.Trim();

    var isInvalid = key.Equals(placeholder, StringComparison.OrdinalIgnoreCase) || key.Length < 32;
    if (!environment.IsDevelopment() && !environment.IsEnvironment("Testing") && isInvalid)
    {
        throw new InvalidOperationException("A secure Jwt:Key value of at least 32 characters is required outside development.");
    }

    return key.Length >= 32 ? key : key.PadRight(32, '_');
}

public partial class Program;
