namespace eNote.Application.Features.Communication.Events;

public static class EventSearchExtensions
{
    public static IQueryable<Event> ApplySearch(this IQueryable<Event> query, EventSearchObject search)
    {
        if (!string.IsNullOrWhiteSpace(search.Title))
        {
            query = query.Where(x => x.Title.Contains(search.Title!));
        }

        if (search.CourseId.HasValue)
        {
            query = query.Where(x => x.CourseId == search.CourseId.Value);
        }

        if (search.InstructorId.HasValue)
        {
            query = query.Where(x => x.InstructorId == search.InstructorId.Value);
        }

        if (search.AddressId.HasValue)
        {
            query = query.Where(x => x.AddressId == search.AddressId.Value);
        }

        if (search.From.HasValue)
        {
            query = query.Where(x => x.StartsAt >= search.From.Value);
        }

        if (search.To.HasValue)
        {
            query = query.Where(x => x.StartsAt <= search.To.Value);
        }

        return query;
    }
}
