using NoorLocator.Application.CenterImages.Dtos;
using NoorLocator.Application.Common.Models;

namespace NoorLocator.Application.CenterImages.Interfaces;

public interface ICenterImageService
{
    Task<OperationResult<IReadOnlyCollection<CenterImageDto>>> GetCenterImagesAsync(int centerId, CancellationToken cancellationToken = default);

    Task<OperationResult<CenterImageDto>> UploadCenterImageAsync(
        UploadCenterImageDto request,
        UploadFilePayload file,
        int userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task<OperationResult<CenterImageDto>> SetPrimaryImageAsync(
        int id,
        int userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task<OperationResult> DeleteCenterImageAsync(
        int id,
        int userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);
}
