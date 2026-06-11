using eNote.Domain.Entities.Base;
using eNote.Domain.Enums;

namespace eNote.Domain.Entities
{
    public class Attendance : AuditableEntity
    {
        public AttendanceStatus AttendanceStatus { get; set; }

        public int StudentId { get; set; }
        public Student Student { get; set; } = null!;
        public int LectureId { get; set; }
        public Lecture Lecture { get; set; } = null!;
    }
}
