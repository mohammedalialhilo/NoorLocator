using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using NoorLocator.Api.Extensions;
using NoorLocator.Api.Middleware;
using NoorLocator.Api.OpenApi;
using NoorLocator.Application;
using NoorLocator.Application.Common.Configuration;
using NoorLocator.Application.Common.Models;
using NoorLocator.Infrastructure;
using NoorLocator.Infrastructure.Persistence;
using NoorLocator.Infrastructure.Seeding;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

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

var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();
var jwtKey = ResolveJwtKey(builder.Environment, jwtSettings.Key);

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

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
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
            Name = "NoorLocator",
            Url = new Uri("https://localhost")
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

if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var initializer = scope.ServiceProvider.GetRequiredService<NoorLocatorDbInitializer>();
    await initializer.InitializeAsync();
}

app.UseMiddleware<ApiExceptionMiddleware>();
app.UseResponseCompression();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

var configuredFrontendPath = builder.Configuration["Frontend:RelativeRootPath"];
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

    app.UseDefaultFiles(new DefaultFilesOptions
    {
        FileProvider = frontendFileProvider
    });

    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = frontendFileProvider
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

var swaggerEnabled = app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled");
if (swaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

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
