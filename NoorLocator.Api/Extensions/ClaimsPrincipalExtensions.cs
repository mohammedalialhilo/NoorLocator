using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace NoorLocator.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int? TryGetUserId(this ClaimsPrincipal principal)
    {
        var rawUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue(ClaimTypes.Name);
        return int.TryParse(rawUserId, out var userId) ? userId : null;
    }

    public static int GetRequiredUserId(this ClaimsPrincipal principal)
    {
        var userId = principal.TryGetUserId();
        if (!userId.HasValue)
        {
            throw new InvalidOperationException("Authenticated user id claim was not found.");
        }

        return userId.Value;
    }

    public static bool IsAdmin(this ClaimsPrincipal principal) => principal.IsInRole("Admin");

    public static string? TryGetSessionId(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(JwtRegisteredClaimNames.Sid);
    }
}
