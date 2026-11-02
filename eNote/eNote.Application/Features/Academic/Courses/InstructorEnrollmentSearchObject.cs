using eNote.Application.Common.Search;

namespace eNote.Application.Features.Academic.Courses;

public sealed class InstructorEnrollmentSearchObject : BaseSearchObject
{
    public EnrollmentStatus? EnrollmentStatus { get; set; }
    public int? CourseId { get; set; }
}
