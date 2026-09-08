namespace eNote.Application.Features.Academic.Courses;

public static class CourseSearchExtensions
{
    public static IQueryable<Course> ApplySearch(this IQueryable<Course> query, CourseSearchObject search)
    {
        if (!string.IsNullOrWhiteSpace(search.Name))
        {
            query = query.Where(c => c.Name.Contains(search.Name!));
        }

        if (search.IsPublished.HasValue)
        {
            query = query.Where(c => c.IsPublished == search.IsPublished.Value);
        }

        if (search.InstructorId.HasValue)
        {
            query = query.Where(c => c.InstructorId == search.InstructorId.Value);
        }

        return query;
    }
}
