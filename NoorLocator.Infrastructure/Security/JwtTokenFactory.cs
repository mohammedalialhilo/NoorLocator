using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NoorLocator.Application.Common.Configuration;
using NoorLocator.Domain.Entities;

namespace NoorLocator.Infrastructure.Security;

public class JwtTokenFactory(IOptions<JwtSettings> jwtOptions)
{
    private readonly JwtSettings _jwtSettings = jwtOptions.Value;
    private const string PlaceholderKey = "CHANGE-ME-TO-A-SECURE-32-CHARACTER-MINIMUM-SECRET";

    public string CreateSessionId()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
    }

    public (string token, DateTime expiresAtUtc) CreateAccessToken(User user, string sessionId)
    {
        var now = DateTime.UtcNow;
        var expiresAtUtc = now.AddMinutes(_jwtSettings.TokenLifetimeMinutes);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(NormalizeJwtKey(_jwtSettings.Key)));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Role, user.Role.ToString()),
            new(JwtRegisteredClaimNames.Sid, sessionId),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            notBefore: now,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc);
    }

    public (string token, DateTime expiresAtUtc) CreateRefreshToken()
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var expiresAtUtc = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenLifetimeDays);
        return (token, expiresAtUtc);
    }

    private static string NormalizeJwtKey(string? configuredKey)
    {
        var key = string.IsNullOrWhiteSpace(configuredKey)
            ? PlaceholderKey
            : configuredKey.Trim();

        return key.Length >= 32 ? key : key.PadRight(32, '_');
    }
}
