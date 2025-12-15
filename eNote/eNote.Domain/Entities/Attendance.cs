using eNote.Domain.Enums;

namespace eNote.Domain.Entities
{
    public class Attendance
    {
        public int Id { get; set; }
        public AttendanceStatus AttendanceStatus { get; set; }

        public int StudentId { get; set; }
        public Student Student { get; set; } = null!;
        public int LectureId { get; set; }
        public Lecture Lecture { get; set; } = null!;
    }
}
