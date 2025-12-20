using eNote.Domain.Entities.Base;
using eNote.Domain.Entities.Users;
using eNote.Domain.Enums;

namespace eNote.Domain.Entities.Courses
{
    public class Enrollment : BaseEntity
    {
        public EnrollmentStatus EnrollmentStatus { get; set; }

        public int StudentId { get; set; }
        public Student Student { get; set; } = null!;
        public int CourseId { get; set; }
        public Course Course { get; set; } = null!;        
    }
}
