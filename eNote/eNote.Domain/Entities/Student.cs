using eNote.Domain.Entities.Base;

namespace eNote.Domain.Entities
{
    public class Student : AuditableEntity
    {
        public int AppUserId
        {
            get; private set;
        }
        public DateTime EnrollmentDate
        {
            get; private set;
        }

        protected Student()
        {
        }

        public Student(int appUserId, DateTime enrollmentDate)
        {
            AppUserId = appUserId;
            EnrollmentDate = enrollmentDate;
        }

        public ICollection<Attendance> Attendances { get; private set; } = new List<Attendance>();
        public ICollection<Enrollment> Enrollments { get; private set; } = new List<Enrollment>();
        public ICollection<InstrumentRental> InstrumentRentals { get; private set; } = new List<InstrumentRental>();
        public ICollection<AssignmentSubmission> AssignmentSubmissions { get; private set; } = new List<AssignmentSubmission>();
    }
}
