namespace eNote.Application.Features.Academic.StudentDirectory;

public sealed class StudentCourseGradeDto
{
    public int CourseId { get; set; }
    public string CourseName { get; set; } = null!;
    public string? InstructorName { get; set; }
    public EnrollmentStatus EnrollmentStatus { get; set; }
    public double? AverageGrade { get; set; }
    public int GradedSubmissions { get; set; }
}
