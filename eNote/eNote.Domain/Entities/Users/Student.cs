using eNote.Domain.Entities.Base;

namespace eNote.Domain.Entities.Users
{
    public class Student : BaseEntity
    {        
        public int UserId { get; set; }        
        public DateTime EnrollmentDate { get; set; }

        protected Student() { }

        public Student(int userId, DateTime enrollmentDate)
        {
            UserId = userId;
            EnrollmentDate = enrollmentDate;
        }

        public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public ICollection<InstrumentRental> InstrumentRentals { get; set; } = new List<InstrumentRental>();
        public ICollection<AssignmentSubmission> AssignmentSubmissions { get; set; } = new List<AssignmentSubmission>();
    }
}
