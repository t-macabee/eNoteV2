using System.ComponentModel.DataAnnotations;

namespace eNote.Application.Features.Academic.Courses;

public class CourseRequest
{
    public required string Name { get; set; }
    public string? Description { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Price must be non-negative.")]
    public decimal Price { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsPublished { get; set; }
}
