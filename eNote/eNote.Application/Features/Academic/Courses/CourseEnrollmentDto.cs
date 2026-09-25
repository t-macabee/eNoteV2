namespace eNote.Application.Features.Academic.Courses;

public class CourseEnrollmentDto
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = null!;
    public EnrollmentStatus EnrollmentStatus { get; set; }
    public DateTime? PaidUntil { get; set; }
    public DateTime? DecidedAt { get; set; }
    public string? DecisionNote { get; set; }
}
