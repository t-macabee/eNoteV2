namespace eNote.Application.Features.Communication.Notifications;

public static class NotificationSearchExtensions
{
    public static IQueryable<Notification> ApplySearch(this IQueryable<Notification> query, NotificationSearchObject search)
    {
        if (search.IsRead.HasValue)
        {
            query = query.Where(x => x.IsRead == search.IsRead.Value);
        }

        return query;
    }
}
