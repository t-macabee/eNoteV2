using eNote.Domain.Entities;

namespace eNote.Application.Features.Lectures;

public static class LectureSearchExtensions
{
    public static IQueryable<Lecture> ApplySearch(this IQueryable<Lecture> query, LectureSearchObject search)
    {
        if (!string.IsNullOrWhiteSpace(search.Name))
        {
            query = query.Where(x => x.Name.Contains(search.Name));
        }

        if (search.LectureType.HasValue)
        {
            query = query.Where(x => x.LectureType == search.LectureType.Value);
        }

        if (search.CourseId.HasValue)
        {
            query = query.Where(x => x.CourseId == search.CourseId.Value);
        }

        if (search.From.HasValue)
        {
            query = query.Where(x => x.LectureTime >= search.From.Value);
        }

        if (search.To.HasValue)
        {
            query = query.Where(x => x.LectureTime <= search.To.Value);
        }

        return query;
    }
}
