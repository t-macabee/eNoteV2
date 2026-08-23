namespace eNote.Domain.Entities.Identity;

public class Instructor : AuditableEntity
{
    public int AppUserId { get; private set; }

    public ICollection<Course> Courses { get; private set; } = [];

    protected Instructor()
    {
    }

    public Instructor(int appUserId)
    {
        AppUserId = appUserId;
    }
}
