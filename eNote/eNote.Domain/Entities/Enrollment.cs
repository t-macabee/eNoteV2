using eNote.Domain.Enums;

namespace eNote.Domain.Entities
{
    public class Enrollment
    {
        public int Id { get; set; }
        public EnrollmentStatus EnrollmentStatus { get; set; }

        public int StudentId { get; set; }
        public Student Student { get; set; } = null!;
        public int CourseId { get; set; }
        public Course Course { get; set; } = null!;        
    }
}
