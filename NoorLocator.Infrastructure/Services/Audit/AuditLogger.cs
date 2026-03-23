using System.Text.Json;
using Microsoft.AspNetCore.Http;
using NoorLocator.Domain.Entities;
using NoorLocator.Infrastructure.Persistence;

namespace NoorLocator.Infrastructure.Services.Audit;

public class AuditLogger(NoorLocatorDbContext dbContext, IHttpContextAccessor httpContextAccessor)
{
    public async Task WriteAsync(
        string action,
        string entityName,
        string? entityId,
        int? userId,
        object? metadata,
        CancellationToken cancellationToken = default)
    {
        var log = new AuditLog
        {
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            UserId = userId,
            Metadata = metadata is null ? null : JsonSerializer.Serialize(metadata),
            IpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
            CreatedAt = DateTime.UtcNow
        };

        dbContext.AuditLogs.Add(log);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
