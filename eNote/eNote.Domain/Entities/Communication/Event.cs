namespace eNote.Domain.Entities.Communication;

public class Event : AuditableEntity
{
    public string Title { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public DateTime StartsAt { get; private set; }
    public DateTime? EndsAt { get; private set; }

    public int? AddressId { get; private set; }
    public Address? Address { get; private set; }

    public int? CourseId { get; private set; }
    public Course? Course { get; private set; }

    public int? InstructorId { get; private set; }
    public Instructor? Instructor { get; private set; }

    protected Event()
    {
    }

    public Event(string title, string description, DateTime startsAt, DateTime? endsAt, int? addressId, int? courseId, int? instructorId)
    {
        Title = title;
        Description = description;
        StartsAt = startsAt;
        EndsAt = endsAt;
        AddressId = addressId;
        CourseId = courseId;
        InstructorId = instructorId;
    }

    public void UpdateDetails(string title, string description, DateTime startsAt, DateTime? endsAt, int? addressId, int? courseId, int? instructorId)
    {
        Title = title;
        Description = description;
        StartsAt = startsAt;
        EndsAt = endsAt;
        AddressId = addressId;
        CourseId = courseId;
        InstructorId = instructorId;
    }
}
