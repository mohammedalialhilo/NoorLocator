using NoorLocator.Application.Common.Models;
using NoorLocator.Application.Majalis.Dtos;

namespace NoorLocator.Application.Majalis.Interfaces;

public interface IMajlisService
{
    Task<OperationResult<IReadOnlyCollection<MajlisDto>>> GetMajalisAsync(int? centerId, CancellationToken cancellationToken = default);

    Task<OperationResult<MajlisDto>> GetMajlisByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<OperationResult> CreateMajlisAsync(CreateMajlisDto request, int userId, bool isAdmin, CancellationToken cancellationToken = default);

    Task<OperationResult> UpdateMajlisAsync(int id, UpdateMajlisDto request, int userId, bool isAdmin, CancellationToken cancellationToken = default);

    Task<OperationResult> DeleteMajlisAsync(int id, int userId, bool isAdmin, CancellationToken cancellationToken = default);
}
