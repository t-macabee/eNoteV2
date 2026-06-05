using eNote.Domain.Entities.Base;
using eNote.Domain.Enums;

namespace eNote.Domain.Entities
{
    public class Lecture : BaseEntity
    {        
        public string Name { get; set; } = null!;
        public string Location { get; set; } = null!;
        public int Duration { get; set; }
        public DateTime LectureTime { get; set; }
        public LectureType LectureType { get; set; }
        public LectureStatus LectureStatus { get; set; }
        public int? Capacity { get; set; }
        public bool IsCancelled { get; set; }
        public byte[]? RowVersion { get; set; }

        public int CourseId { get; set; }
        public Course Course { get; set; } = null!;

        public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
        public ICollection<LectureNote> LectureNotes { get; set; } = new List<LectureNote>();
        public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
    }
}
