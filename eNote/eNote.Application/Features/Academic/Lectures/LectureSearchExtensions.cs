using eNote.Application.Common.Search;

namespace eNote.Application.Features.Academic.Lectures;

public static class LectureSearchExtensions
{
    public static IQueryable<Lecture> ApplySearch(this IQueryable<Lecture> query, LectureSearchObject search)
    {
        var from = search.From.ToUtc();
        var to = search.To.ToUtc();

        if (!string.IsNullOrWhiteSpace(search.Name))
        {
            query = query.Where(x => x.Name.Contains(search.Name!));
        }

        if (search.LectureType.HasValue)
        {
            query = query.Where(x => x.LectureType == search.LectureType.Value);
        }

        if (search.CourseId.HasValue)
        {
            query = query.Where(x => x.CourseId == search.CourseId.Value);
        }

        if (from.HasValue)
        {
            query = query.Where(x => x.LectureTime >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(x => x.LectureTime <= to.Value);
        }

        return query;
    }
}
