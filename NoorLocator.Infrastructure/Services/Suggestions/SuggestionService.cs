using Microsoft.EntityFrameworkCore;
using NoorLocator.Application.Common.Models;
using NoorLocator.Application.Suggestions.Dtos;
using NoorLocator.Application.Suggestions.Interfaces;
using NoorLocator.Domain.Entities;
using NoorLocator.Domain.Enums;
using NoorLocator.Infrastructure.Persistence;
using NoorLocator.Infrastructure.Services.Audit;

namespace NoorLocator.Infrastructure.Services.Suggestions;

public class SuggestionService(NoorLocatorDbContext dbContext, AuditLogger auditLogger) : ISuggestionService
{
    public async Task<OperationResult> CreateAsync(CreateSuggestionDto request, int userId, CancellationToken cancellationToken = default)
    {
        var suggestion = new Suggestion
        {
            UserId = userId,
            Message = request.Message.Trim(),
            Type = request.Type,
            Status = SuggestionReviewStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Suggestions.Add(suggestion);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLogger.WriteAsync(
            action: "SuggestionSubmitted",
            entityName: nameof(Suggestion),
            entityId: suggestion.Id.ToString(),
            userId: userId,
            metadata: new
            {
                suggestion.Type,
                suggestion.Status
            },
            cancellationToken: cancellationToken);

        return OperationResult.Accepted("Suggestion submitted for admin review.");
    }

    public async Task<OperationResult<IReadOnlyCollection<SuggestionDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var suggestions = await dbContext.Suggestions
            .AsNoTracking()
            .OrderByDescending(suggestion => suggestion.CreatedAt)
            .Select(suggestion => new SuggestionDto
            {
                Id = suggestion.Id,
                UserId = suggestion.UserId,
                Message = suggestion.Message,
                Type = suggestion.Type,
                Status = suggestion.Status,
                CreatedAt = suggestion.CreatedAt
            })
            .ToArrayAsync(cancellationToken);

        return OperationResult<IReadOnlyCollection<SuggestionDto>>.Success(suggestions);
    }
}
