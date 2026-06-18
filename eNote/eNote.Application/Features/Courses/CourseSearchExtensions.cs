using eNote.Domain.Entities;

namespace eNote.Application.Features.Courses;

public static class CourseSearchExtensions
{
    public static IQueryable<Course> ApplySearch(this IQueryable<Course> query, CourseSearchObject search)
    {
        if (!string.IsNullOrWhiteSpace(search.Name))
        {
            query = query.Where(c => c.Name.Contains(search.Name));
        }

        if (search.IsPublished.HasValue)
        {
            query = query.Where(c => c.IsPublished == search.IsPublished.Value);
        }

        return query;
    }
}
