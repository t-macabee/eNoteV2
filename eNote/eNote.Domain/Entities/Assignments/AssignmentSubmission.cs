namespace eNote.Domain.Entities;

public class AssignmentSubmission : AuditableEntity
{
    public int AssignmentId { get; private set; }
    public Assignment Assignment { get; private set; } = null!;
    public int StudentId { get; private set; }
    public Student Student { get; private set; } = null!;

    public string? FilePath { get; private set; }
    public DateTime? SubmittedAt { get; private set; }
    public int? Grade { get; private set; }

    protected AssignmentSubmission()
    {
    }

    public AssignmentSubmission(int assignmentId, int studentId)
    {
        AssignmentId = assignmentId;
        StudentId = studentId;
    }

    public void Submit(string? filePath, DateTime submittedAt)
    {
        FilePath = filePath;
        SubmittedAt = submittedAt;
    }

    public void SetGrade(int grade)
    {
        Grade = grade;
    }
}
