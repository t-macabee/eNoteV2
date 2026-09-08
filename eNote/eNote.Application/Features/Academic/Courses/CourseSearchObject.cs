using eNote.Application.Common.Search;

namespace eNote.Application.Features.Academic.Courses;

public class CourseSearchObject : BaseSearchObject
{
    public string? Name { get; set; }
    public bool? IsPublished { get; set; }
    public int? InstructorId { get; set; }
}
