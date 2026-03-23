using NoorLocator.Application.Admin.Dtos;
using NoorLocator.Application.Centers.Dtos;
using NoorLocator.Application.Common.Models;

namespace NoorLocator.Application.Admin.Interfaces;

public interface IAdminService
{
    Task<OperationResult<AdminDashboardDto>> GetDashboardAsync(CancellationToken cancellationToken = default);

    Task<OperationResult<IReadOnlyCollection<AdminCenterRequestDto>>> GetCenterRequestsAsync(CancellationToken cancellationToken = default);

    Task<OperationResult> ApproveCenterRequestAsync(int id, int adminUserId, CancellationToken cancellationToken = default);

    Task<OperationResult> RejectCenterRequestAsync(int id, int adminUserId, CancellationToken cancellationToken = default);

    Task<OperationResult<IReadOnlyCollection<AdminManagerRequestDto>>> GetManagerRequestsAsync(CancellationToken cancellationToken = default);

    Task<OperationResult> ApproveManagerRequestAsync(int id, int adminUserId, CancellationToken cancellationToken = default);

    Task<OperationResult> RejectManagerRequestAsync(int id, int adminUserId, CancellationToken cancellationToken = default);

    Task<OperationResult<IReadOnlyCollection<AdminCenterLanguageSuggestionDto>>> GetCenterLanguageSuggestionsAsync(CancellationToken cancellationToken = default);

    Task<OperationResult> ApproveCenterLanguageSuggestionAsync(int id, int adminUserId, CancellationToken cancellationToken = default);

    Task<OperationResult> RejectCenterLanguageSuggestionAsync(int id, int adminUserId, CancellationToken cancellationToken = default);

    Task<OperationResult<IReadOnlyCollection<AdminSuggestionDto>>> GetSuggestionsAsync(CancellationToken cancellationToken = default);

    Task<OperationResult> ReviewSuggestionAsync(int id, int adminUserId, CancellationToken cancellationToken = default);

    Task<OperationResult<IReadOnlyCollection<AdminUserDto>>> GetUsersAsync(CancellationToken cancellationToken = default);

    Task<OperationResult<IReadOnlyCollection<AdminCenterDto>>> GetCentersAsync(CancellationToken cancellationToken = default);

    Task<OperationResult> UpdateCenterAsync(int id, UpdateCenterDto request, int adminUserId, CancellationToken cancellationToken = default);

    Task<OperationResult> DeleteCenterAsync(int id, int adminUserId, CancellationToken cancellationToken = default);

    Task<OperationResult<IReadOnlyCollection<AdminAuditLogDto>>> GetAuditLogsAsync(CancellationToken cancellationToken = default);
}
