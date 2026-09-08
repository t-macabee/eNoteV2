namespace eNote.Application.Features.Identity.Students;

public class StudentEnrollmentDto
{
    public int CourseId { get; set; }
    public string CourseName { get; set; } = null!;
    public int InstructorId { get; set; }
    public string? InstructorName { get; set; }
}
