using eNote.Domain.Entities;

namespace eNote.Domain.Entities;

public class Assignment : AuditableEntity
{
    public int LectureId { get; private set; }
    public Lecture Lecture { get; private set; } = null!;

    public string Title { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public DateTime DueAt { get; private set; }
    public bool IsActive { get; private set; } = true;

    public ICollection<AssignmentSubmission> AssignmentSubmissions { get; private set; } = new List<AssignmentSubmission>();

    protected Assignment()
    {
    }

    public Assignment(string title, string description, DateTime dueAt, int lectureId)
    {
        Title = title;
        Description = description;
        DueAt = dueAt;
        LectureId = lectureId;
        IsActive = true;
    }

    public void UpdateDetails(string title, string description, DateTime dueAt)
    {
        Title = title;
        Description = description;
        DueAt = dueAt;
    }

    public void SoftDelete()
    {
        IsActive = false;
    }
}
