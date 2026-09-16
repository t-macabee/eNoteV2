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

    /// <returns>The period start (the later of the previous <see cref="PaidUntil"/> or <paramref name="utcNow"/>) and the new <see cref="PaidUntil"/>, so callers don't have to re-derive them.</returns>
    public (DateTime Start, DateTime End) ExtendPaidUntil(DateTime utcNow, int days)
    {
        if (days <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(days), "Extension days must be positive.");
        }

        var periodStart = PaidUntil.HasValue && PaidUntil.Value > utcNow ? PaidUntil.Value : utcNow;
        PaidUntil = periodStart.AddDays(days);
        return (periodStart, PaidUntil.Value);
    }
}
