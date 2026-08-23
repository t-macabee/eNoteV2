namespace eNote.Domain.Entities.Identity;

public sealed class Student : AuditableEntity
{
    public int AppUserId { get; private set; }
    public DateTime EnrollmentDate { get; private set; }
    public DateTime? MembershipPaidUntil { get; private set; }

    public ICollection<Attendance> Attendances { get; private set; } = [];
    public ICollection<Enrollment> Enrollments { get; private set; } = [];
    public ICollection<InstrumentRental> InstrumentRentals { get; private set; } = [];
    public ICollection<AssignmentSubmission> AssignmentSubmissions { get; private set; } = [];

    private Student()
    {
    }

    public Student(int appUserId, DateTime enrollmentDate)
    {
        AppUserId = appUserId;
        EnrollmentDate = enrollmentDate;
    }

    public void UpdateMembership(DateTime? paidUntil)
    {
        MembershipPaidUntil = paidUntil;
    }

    public bool HasActiveMembership(DateTime utcNow)
    {
        return MembershipPaidUntil.HasValue && MembershipPaidUntil.Value.Date >= utcNow.Date;
    }
}
