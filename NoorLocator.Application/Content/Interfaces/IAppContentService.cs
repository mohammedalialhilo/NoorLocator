using NoorLocator.Application.Common.Models;
using NoorLocator.Application.Content.Dtos;

namespace NoorLocator.Application.Content.Interfaces;

public interface IAppContentService
{
    Task<OperationResult<AboutContentDto>> GetAboutContentAsync(string? languageCode, CancellationToken cancellationToken = default);
}
