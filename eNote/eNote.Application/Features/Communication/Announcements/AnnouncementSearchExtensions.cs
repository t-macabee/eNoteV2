namespace eNote.Application.Features.Communication.Announcements;

public static class AnnouncementSearchExtensions
{
    public static IQueryable<Announcement> ApplySearch(this IQueryable<Announcement> query, AnnouncementSearchObject search)
    {
        if (!string.IsNullOrWhiteSpace(search.Title))
        {
            query = query.Where(x => x.Title.Contains(search.Title!));
        }

        return query;
    }
}
