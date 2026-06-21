using eNote.Application.Common.Search;
using eNote.Domain.Entities;

namespace eNote.Application.Features.Notifications;

public static class NotificationSearchExtensions
{
    public static IQueryable<Notification> ApplySearch(this IQueryable<Notification> query, NotificationSearchObject search) =>
        query.WhereEqualsIf(search.IsRead, x => x.IsRead == search.IsRead!.Value);
}