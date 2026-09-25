namespace eNote.Application.Features.Academic.Courses;

public class CourseDto
{
    public int Id { get; set; }
    public int InstructorId { get; set; }

    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsPublished { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public decimal Price { get; set; }

    public int EnrolledCount { get; set; }

    public bool IsEnrolled { get; set; }
    public int? EnrollmentId { get; set; }
    public DateTime? PaidUntil { get; set; }
    public EnrollmentStatus? EnrollmentStatus { get; set; }
    public string? EnrollmentDecisionNote { get; set; }
    public bool IsFree => Price == 0;

    public string? InstructorName { get; set; }
}
