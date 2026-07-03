using eNote.Application.Common.Search;

namespace eNote.Application.Features.Academic.Lectures;

public static class LectureSearchExtensions
{
    public static IQueryable<Lecture> ApplySearch(this IQueryable<Lecture> query, LectureSearchObject search) =>
        query
            .WhereContainsIf(search.Name, x => x.Name.Contains(search.Name!))
            .WhereEqualsIf(search.LectureType, x => x.LectureType == search.LectureType!.Value)
            .WhereEqualsIf(search.CourseId, x => x.CourseId == search.CourseId!.Value)
            .WhereEqualsIf(search.From, x => x.LectureTime >= search.From!.Value)
            .WhereEqualsIf(search.To, x => x.LectureTime <= search.To!.Value);
}