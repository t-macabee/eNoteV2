namespace eNote.Application.Features.Academic.Assignments;

public class AssignmentSubmissionDto
{
    public int Id { get; set; }
    public int AssignmentId { get; set; }
    public int StudentId { get; set; }

    public string? StudentName { get; set; }
    public string? FilePath { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public int? Grade { get; set; }
}
