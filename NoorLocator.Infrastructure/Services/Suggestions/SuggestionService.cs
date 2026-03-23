using Microsoft.EntityFrameworkCore;
using NoorLocator.Application.Common.Models;
using NoorLocator.Application.Suggestions.Dtos;
using NoorLocator.Application.Suggestions.Interfaces;
using NoorLocator.Domain.Entities;
using NoorLocator.Domain.Enums;
using NoorLocator.Infrastructure.Persistence;

namespace NoorLocator.Infrastructure.Services.Suggestions;

public class SuggestionService(NoorLocatorDbContext dbContext) : ISuggestionService
{
    public async Task<OperationResult> CreateAsync(CreateSuggestionDto request, int userId, CancellationToken cancellationToken = default)
    {
        dbContext.Suggestions.Add(new Suggestion
        {
            UserId = userId,
            Message = request.Message.Trim(),
            Type = request.Type,
            Status = SuggestionReviewStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
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
