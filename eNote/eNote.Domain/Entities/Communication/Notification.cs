namespace eNote.Domain.Entities.Communication;

public sealed class Notification
{
    public int Id { get; private set; }
    public int UserId { get; private set; }
    public int? RentalId { get; private set; }
    public int? LectureId { get; private set; }
    public int? SubmissionId { get; private set; }

    public string Title { get; private set; } = null!;
    public string Body { get; private set; } = null!;

    public bool IsRead { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Notification()
    {
    }

    public Notification(int userId, string title, string body, DateTime createdAt, int? rentalId = null, int? lectureId = null, int? submissionId = null)
    {
        UserId = userId;
        Title = title;
        Body = body;
        CreatedAt = createdAt;
        RentalId = rentalId;
        LectureId = lectureId;
        SubmissionId = submissionId;
    }

    public void MarkRead()
    {
        IsRead = true;
    }
}
