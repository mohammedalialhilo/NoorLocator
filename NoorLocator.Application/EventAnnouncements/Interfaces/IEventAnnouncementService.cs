using NoorLocator.Application.Common.Models;
using NoorLocator.Application.EventAnnouncements.Dtos;

namespace NoorLocator.Application.EventAnnouncements.Interfaces;

public interface IEventAnnouncementService
{
    Task<OperationResult<IReadOnlyCollection<EventAnnouncementDto>>> GetAnnouncementsAsync(
        int centerId,
        int? userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task<OperationResult<EventAnnouncementDto>> GetAnnouncementByIdAsync(
        int id,
        int? userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task<OperationResult<EventAnnouncementDto>> CreateAnnouncementAsync(
        CreateEventAnnouncementDto request,
        UploadFilePayload? image,
        int userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task<OperationResult<EventAnnouncementDto>> UpdateAnnouncementAsync(
        int id,
        UpdateEventAnnouncementDto request,
        UploadFilePayload? image,
        int userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task<OperationResult> DeleteAnnouncementAsync(
        int id,
        int userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);
}
