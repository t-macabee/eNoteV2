using eNote.Domain.Entities.Base;
using eNote.Domain.Enums;

namespace eNote.Domain.Entities
{
    public class Enrollment : AuditableEntity
    {
        public int StudentId { get; private set; }
        public Student Student { get; private set; } = null!;
        public int CourseId { get; private set; }
        public Course Course { get; private set; } = null!;

        public EnrollmentStatus EnrollmentStatus { get; private set; }

        protected Enrollment()
        {
        }

        public Enrollment(int studentId, int courseId, EnrollmentStatus status)
        {
            StudentId = studentId;
            CourseId = courseId;
            EnrollmentStatus = status;
        }

        public void UpdateStatus(EnrollmentStatus status)
        {
            EnrollmentStatus = status;
        }
    }
}
