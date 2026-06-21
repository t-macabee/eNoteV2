using eNote.Application.Common.Paging;

namespace eNote.Application.Features.Notifications.Services;

public interface INotificationService
{
    Task<PagedResult<NotificationDto>> GetPagedAsync(NotificationSearchObject search, CancellationToken cancellationToken = default);

    Task<NotificationUnreadCountDto> GetUnreadCountAsync(CancellationToken cancellationToken = default);

    Task<NotificationDto> MarkReadAsync(int id, CancellationToken cancellationToken = default);

    Task<NotificationUnreadCountDto> MarkAllReadAsync(CancellationToken cancellationToken = default);
}
