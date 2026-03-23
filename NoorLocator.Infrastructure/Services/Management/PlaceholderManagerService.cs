using NoorLocator.Application.Common.Models;
using NoorLocator.Application.Languages.Dtos;
using NoorLocator.Application.Management.Dtos;
using NoorLocator.Application.Management.Interfaces;

namespace NoorLocator.Infrastructure.Services.Management;

public class PlaceholderManagerService : IManagerService
{
    public Task<OperationResult> ApproveCenterLanguageSuggestionAsync(ApproveLanguageSuggestionDto request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            OperationResult.Accepted(
                "Language suggestion approval is scaffolded for the admin workflow."));
    }

    public Task<OperationResult> ApproveManagerAsync(ApproveManagerRequestDto request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            OperationResult.Accepted(
                "Manager approval is scaffolded. Persistence and authorization rules will follow in the next phase."));
    }

    public Task<OperationResult> RequestManagerAccessAsync(ManagerRequestDto request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            OperationResult.Accepted(
                "Manager request intake is scaffolded and ready for Phase 2 business rules."));
    }

    public Task<OperationResult> SuggestCenterLanguageAsync(CreateCenterLanguageSuggestionDto request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            OperationResult.Accepted(
                "Center language suggestion intake is scaffolded for the moderated workflow."));
    }
}
