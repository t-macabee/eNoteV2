namespace eNote.Application.Features.Courses;

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
}
