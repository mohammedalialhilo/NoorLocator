using NoorLocator.Application.Common.Models;

namespace NoorLocator.Infrastructure.Services.Media;

public interface IMediaStorageService
{
    Task<OperationResult<StoredMediaFile>> SaveImageAsync(
        UploadFilePayload file,
        string category,
        CancellationToken cancellationToken = default);

    Task DeleteFileAsync(string? publicUrl, CancellationToken cancellationToken = default);
}
