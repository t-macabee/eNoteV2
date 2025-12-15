namespace eNote.Domain.Entities
{
    public class Student
    {
        public int Id { get; set; }
        public DateTime EnrollmentDate { get; set; }

        public int AppUserId { get; set; }        

        public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public ICollection<InstrumentRental> InstrumentRentals { get; set; } = new List<InstrumentRental>();
        public ICollection<AssignmentSubmission> AssignmentSubmissions { get; set; } = new List<AssignmentSubmission>();
    }
}
