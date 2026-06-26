using System.ComponentModel.DataAnnotations;

namespace eNote.Application.Features.Academic.Assignments;

public class AssignmentRequest
{
    [Required]
    public string Title { get; set; } = null!;
    [Required]
    public string Description { get; set; } = null!;
    [Required]
    public DateTime DueAt { get; set; }
}
