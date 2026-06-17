using eNote.Domain.Entities.Base;
using eNote.Domain.Enums;

namespace eNote.Domain.Entities
{
    public class Attendance : AuditableEntity
    {
        public AttendanceStatus AttendanceStatus
        {
            get; private set;
        }

        public int StudentId
        {
            get; private set;
        }
        public Student Student { get; private set; } = null!;

        public int LectureId
        {
            get; private set;
        }
        public Lecture Lecture { get; private set; } = null!;

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
}
