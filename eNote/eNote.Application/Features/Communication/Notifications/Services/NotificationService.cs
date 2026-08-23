using MapsterMapper;

namespace eNote.Application.Features.Communication.Notifications.Services;

public sealed class NotificationService(IAppDbContext context, IMapper mapper, ICurrentUserContext currentUser)
{
    public async Task<PagedResult<NotificationDto>> GetPagedAsync(NotificationSearchObject search, CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;

        var query = context.Set<Notification>()
            .AsNoTracking()
            .Where(x => x.UserId == userId);

        query = query.ApplySearch(search);

        return await query.ToPagedResultAsync(
            search,
            mapper.Map<NotificationDto>,
            q => q.OrderByDescending(x => x.CreatedAt),
            cancellationToken);
    }

    public async Task<NotificationUnreadCountDto> GetUnreadCountAsync(CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;

        var count = await context.Set<Notification>()
            .AsNoTracking()
            .CountAsync(x => x.UserId == userId && !x.IsRead, cancellationToken);

        return new NotificationUnreadCountDto { UnreadCount = count };
    }

    public async Task<NotificationDto> MarkReadAsync(int id, CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;

        var notification = await context.Set<Notification>()
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
        var userId = currentUser.UserId;

        await context.Set<Notification>()
            .Where(x => x.UserId == userId && !x.IsRead)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.IsRead, true), cancellationToken);

        return await GetUnreadCountAsync(cancellationToken);
    }
}
