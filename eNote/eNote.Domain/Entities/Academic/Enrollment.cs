using eNote.Domain.Enums;
namespace eNote.Domain.Entities.Academic;

public class Enrollment : AuditableEntity
{
    public int StudentId { get; private set; }
    public Student Student { get; private set; } = null!;
    public int CourseId { get; private set; }
    public Course Course { get; private set; } = null!;

    public EnrollmentStatus EnrollmentStatus { get; private set; }
    public DateTime? PaidUntil { get; private set; }

    public ICollection<CoursePayment> Payments { get; private set; } = [];

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

    public void ExtendPaidUntil(DateTime utcNow, int days)
    {
        if (days <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(days), "Extension days must be positive.");
        }

        var baseDate = PaidUntil.HasValue && PaidUntil.Value > utcNow ? PaidUntil.Value : utcNow;
        PaidUntil = baseDate.AddDays(days);
    }

    // Exact-timestamp check by design, unlike Student.HasActiveMembership's .Date truncation.
    public bool HasPaidAccess(DateTime utcNow) => PaidUntil.HasValue && PaidUntil.Value >= utcNow;
}
