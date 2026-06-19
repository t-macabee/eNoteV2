using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Paging;
using eNote.Application.Common.Persistence;
using eNote.Domain.Entities;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.Notifications.Services;

public sealed class NotificationService(IAppDbContext context, IMapper mapper, ICurrentUserService currentUserService) : INotificationService
{
    public async Task<PagedResult<NotificationDto>> GetPagedAsync(NotificationSearchObject search, CancellationToken cancellationToken = default)
    {
        int userId = currentUserService.UserId;

        IQueryable<Notification> query = context.Set<Notification>()
            .AsNoTracking()
            .Where(x => x.UserId == userId);

        query = query.ApplySearch(search);

        return await query.ToPagedResultAsync(
            search.Page,
            search.PageSize,
            search.IncludeTotalCount,
            mapper.Map<NotificationDto>,
            q => q.OrderByDescending(x => x.CreatedAt),
            cancellationToken);
    }

    public async Task<NotificationUnreadCountDto> GetUnreadCountAsync(CancellationToken cancellationToken = default)
    {
        int userId = currentUserService.UserId;

        int count = await context.Set<Notification>()
            .AsNoTracking()
            .CountAsync(x => x.UserId == userId && !x.IsRead, cancellationToken);

        return new NotificationUnreadCountDto { UnreadCount = count };
    }

    public async Task<NotificationDto> MarkReadAsync(int id, CancellationToken cancellationToken = default)
    {
        int userId = currentUserService.UserId;

        Notification notification = await context.Set<Notification>()
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken)
            ?? throw new NotFoundException(Messages.NotificationNotFound);

        if (!notification.IsRead)
        {
            notification.MarkRead();
            await context.SaveChangesAsync(cancellationToken);
        }

        return mapper.Map<NotificationDto>(notification);
    }

    public async Task<NotificationUnreadCountDto> MarkAllReadAsync(CancellationToken cancellationToken = default)
    {
        int userId = currentUserService.UserId;

        await context.Set<Notification>()
            .Where(x => x.UserId == userId && !x.IsRead)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.IsRead, true), cancellationToken);

        return await GetUnreadCountAsync(cancellationToken);
    }
}
