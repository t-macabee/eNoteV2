using eNote.Application.Common.Search;
using eNote.Domain.Entities;

namespace eNote.Application.Features.Academic.Courses;

public static class CourseSearchExtensions
{
    public static IQueryable<Course> ApplySearch(this IQueryable<Course> query, CourseSearchObject search) =>
        query
            .WhereContainsIf(search.Name, c => c.Name.Contains(search.Name!))
            .WhereEqualsIf(search.IsPublished, c => c.IsPublished == search.IsPublished!.Value);
}