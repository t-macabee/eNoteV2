namespace eNote.Application.Features.Academic.Assignments;

public class AssignmentRequest
{
    public required string Title { get; set; }
    public required string Description { get; set; }
    public DateTime DueAt { get; set; }
}
