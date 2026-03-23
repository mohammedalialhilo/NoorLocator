using System.Security.Claims;

namespace NoorLocator.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int GetRequiredUserId(this ClaimsPrincipal principal)
    {
        var rawUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue(ClaimTypes.Name);
        if (!int.TryParse(rawUserId, out var userId))
        {
            throw new InvalidOperationException("Authenticated user id claim was not found.");
        }

        return userId;
    }

    public static bool IsAdmin(this ClaimsPrincipal principal) => principal.IsInRole("Admin");
}
