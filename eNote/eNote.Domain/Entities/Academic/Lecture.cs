using eNote.Domain.Enums;
namespace eNote.Domain.Entities.Academic;

public sealed class Lecture : AuditableEntity
{
    public int CourseId { get; private set; }
    public Course Course { get; private set; } = null!;

    public string Name { get; private set; } = null!;
    public string Location { get; private set; } = null!;
    public LectureType LectureType { get; private set; }

    public DateTime LectureTime { get; private set; }

    /// <summary>Duration in minutes.</summary>
    public int Duration { get; private set; }
    public int? Capacity { get; private set; }

    public LectureStatus LectureStatus { get; private set; }
    public bool IsCancelled => LectureStatus == LectureStatus.Cancelled;
    public bool IsActive { get; private set; } = true;
    public uint Version { get; private set; }

    public ICollection<Attendance> Attendances { get; private set; } = [];
    public ICollection<LectureNote> LectureNotes { get; private set; } = [];
    public ICollection<Assignment> Assignments { get; private set; } = [];

    private Lecture()
    {
    }

    public Lecture(string name, string location, int duration, DateTime lectureTime, LectureType lectureType, int? capacity, int courseId)
    {
        Name = name;
        Location = location;
        Duration = duration;
        LectureTime = lectureTime;
        LectureType = lectureType;
        Capacity = capacity;
        CourseId = courseId;
        LectureStatus = LectureStatus.Scheduled;
        IsActive = true;
    }

    public void UpdateDetails(string name, string location, int duration, DateTime lectureTime, int? capacity)
    {
        Name = name;
        Location = location;
        Duration = duration;
        LectureTime = lectureTime;
        Capacity = capacity;
    }

    public void Cancel()
    {
        LectureStatus = LectureStatus.Cancelled;
    }

    public void SoftDelete()
    {
        IsActive = false;
    }
}
