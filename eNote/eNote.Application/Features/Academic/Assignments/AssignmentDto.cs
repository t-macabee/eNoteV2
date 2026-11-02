namespace eNote.Application.Features.Academic.Assignments;

public class AssignmentDto
{
    public int Id { get; set; }
    public int LectureId { get; set; }

    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;

    public DateTime DueAt { get; set; }

    public DateTime? MySubmittedAt { get; set; }
    public int? MyGrade { get; set; }
}
