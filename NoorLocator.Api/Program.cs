using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using NoorLocator.Application;
using NoorLocator.Application.Common.Configuration;
using NoorLocator.Application.Common.Models;
using NoorLocator.Infrastructure;
using NoorLocator.Infrastructure.Seeding;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var message = context.ModelState.Values
                          .SelectMany(value => value.Errors)
                          .Select(error => error.ErrorMessage)
                          .FirstOrDefault()
                      ?? "Validation failed.";

        return new BadRequestObjectResult(ApiResponse<object?>.Failure(message));
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks();

var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();
var jwtKey = string.IsNullOrWhiteSpace(jwtSettings.Key)
    ? "CHANGE-ME-TO-A-SECURE-32-CHARACTER-MINIMUM-SECRET"
    : jwtSettings.Key;

if (jwtKey.Length < 32)
{
    jwtKey = jwtKey.PadRight(32, '_');
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
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
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ManagerArea", policy =>
        policy.RequireRole("Manager", "Admin"));
});

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        if (allowedOrigins.Length == 0)
        {
            policy.AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();

            return;
        }

        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "NoorLocator API",
        Version = "v1",
        Description = "Phase 1 scaffold for the NoorLocator moderated center discovery platform."
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
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<NoorLocatorDbInitializer>();
    await initializer.InitializeAsync();
}

var frontendRelativePath = builder.Configuration["Frontend:RelativeRootPath"] ?? "..\\frontend";
var frontendPath = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, frontendRelativePath));

if (Directory.Exists(frontendPath))
{
    var frontendFileProvider = new PhysicalFileProvider(frontendPath);

    app.UseDefaultFiles(new DefaultFilesOptions
    {
        FileProvider = frontendFileProvider
    });

    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = frontendFileProvider
    });
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
