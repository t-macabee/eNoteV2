using eNote.Domain.Enums;
using eNote.Domain.Entities.Shared.Base;

using eNote.Domain.Entities.Identity;
namespace eNote.Domain.Entities.Academic;

public class Attendance : AuditableEntity
{
    public int StudentId { get; private set; }
    public Student Student { get; private set; } = null!;
    public int LectureId { get; private set; }
    public Lecture Lecture { get; private set; } = null!;

    public AttendanceStatus AttendanceStatus { get; private set; }

    protected Attendance()
    {
    }

    public Attendance(int studentId, int lectureId, AttendanceStatus status)
    {
        StudentId = studentId;
        LectureId = lectureId;
        AttendanceStatus = status;
    }

    public void UpdateStatus(AttendanceStatus status)
    {
        AttendanceStatus = status;
    }
}
