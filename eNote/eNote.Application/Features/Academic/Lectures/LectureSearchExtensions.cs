using eNote.Application.Common.Search;

namespace eNote.Application.Features.Academic.Lectures;

public static class LectureSearchExtensions
{
    public static IQueryable<Lecture> ApplySearch(this IQueryable<Lecture> query, LectureSearchObject search)
    {
        var from = search.From.HasValue ? DateTime.SpecifyKind(search.From.Value, DateTimeKind.Utc) : (DateTime?)null;
        var to = search.To.HasValue ? DateTime.SpecifyKind(search.To.Value, DateTimeKind.Utc) : (DateTime?)null;

        return query
            .WhereContainsIf(search.Name, x => x.Name.Contains(search.Name!))
            .WhereEqualsIf(search.LectureType, x => x.LectureType == search.LectureType!.Value)
            .WhereEqualsIf(search.CourseId, x => x.CourseId == search.CourseId!.Value)
            .WhereEqualsIf(from, x => x.LectureTime >= from!.Value)
            .WhereEqualsIf(to, x => x.LectureTime <= to!.Value);
    }
}