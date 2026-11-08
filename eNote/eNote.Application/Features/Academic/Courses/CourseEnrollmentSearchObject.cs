using eNote.Application.Common.Search;

namespace eNote.Application.Features.Academic.Courses;

public sealed class CourseEnrollmentSearchObject : BaseSearchObject
{
    public EnrollmentStatus? EnrollmentStatus { get; set; }
    public string? StudentName { get; set; }
}
