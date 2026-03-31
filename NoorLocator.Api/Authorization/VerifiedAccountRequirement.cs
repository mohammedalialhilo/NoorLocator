using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using NoorLocator.Api.Extensions;
using NoorLocator.Infrastructure.Persistence;

namespace NoorLocator.Api.Authorization;

public class VerifiedAccountRequirement : IAuthorizationRequirement;

public class VerifiedAccountHandler(NoorLocatorDbContext dbContext) : AuthorizationHandler<VerifiedAccountRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, VerifiedAccountRequirement requirement)
    {
        var principal = context.User;
        var userId = principal.TryGetUserId();
        if (!userId.HasValue)
        {
            return;
        }

        var verified = await dbContext.Users
            .AsNoTracking()
            .AnyAsync(user => user.Id == userId.Value && user.IsEmailVerified);

        if (verified)
        {
            context.Succeed(requirement);
        }
    }
}
