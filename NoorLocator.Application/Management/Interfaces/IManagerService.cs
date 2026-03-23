using NoorLocator.Application.Common.Models;
using NoorLocator.Application.Languages.Dtos;
using NoorLocator.Application.Management.Dtos;

namespace NoorLocator.Application.Management.Interfaces;

public interface IManagerService
{
    Task<OperationResult> RequestManagerAccessAsync(ManagerRequestDto request, CancellationToken cancellationToken = default);

    Task<OperationResult> ApproveManagerAsync(ApproveManagerRequestDto request, CancellationToken cancellationToken = default);

    Task<OperationResult> SuggestCenterLanguageAsync(CreateCenterLanguageSuggestionDto request, CancellationToken cancellationToken = default);

    Task<OperationResult> ApproveCenterLanguageSuggestionAsync(ApproveLanguageSuggestionDto request, CancellationToken cancellationToken = default);
}
