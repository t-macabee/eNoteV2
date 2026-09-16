namespace eNote.Application.Features.Academic.Courses;

public class CourseRequest
{
    public required string Name { get; set; }
    public string? Description { get; set; }

    public decimal Price { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsPublished { get; set; }
}
