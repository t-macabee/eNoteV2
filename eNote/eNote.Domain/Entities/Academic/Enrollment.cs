using eNote.Domain.Enums;
using eNote.Domain.Shared;

namespace eNote.Domain.Entities.Academic;

public class Enrollment : AuditableEntity
{
    public const int DecisionNoteMaxLength = 500;

    public int StudentId { get; private set; }
    public Student Student { get; private set; } = null!;
    public int CourseId { get; private set; }
    public Course Course { get; private set; } = null!;

    public EnrollmentStatus EnrollmentStatus { get; private set; }
    public DateTime? PaidUntil { get; private set; }
    public int? DecidedById { get; private set; }
    public DateTime? DecidedAt { get; private set; }
    public string? DecisionNote { get; private set; }
    public byte[] Version { get; private set; } = null!;

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

    private static readonly IReadOnlyDictionary<(EnrollmentStatus From, EnrollmentTrigger Trigger), EnrollmentStatus> Transitions =
        new Dictionary<(EnrollmentStatus, EnrollmentTrigger), EnrollmentStatus>
        {
            [(EnrollmentStatus.Canceled, EnrollmentTrigger.Request)] = EnrollmentStatus.Pending,
            [(EnrollmentStatus.Rejected, EnrollmentTrigger.Request)] = EnrollmentStatus.Pending,
            [(EnrollmentStatus.Pending, EnrollmentTrigger.Approve)] = EnrollmentStatus.Active,
            [(EnrollmentStatus.Pending, EnrollmentTrigger.Reject)] = EnrollmentStatus.Rejected,
            [(EnrollmentStatus.Pending, EnrollmentTrigger.Cancel)] = EnrollmentStatus.Canceled,
            [(EnrollmentStatus.Active, EnrollmentTrigger.Cancel)] = EnrollmentStatus.Canceled,
            [(EnrollmentStatus.Active, EnrollmentTrigger.Complete)] = EnrollmentStatus.Completed,
            [(EnrollmentStatus.Canceled, EnrollmentTrigger.Resume)] = EnrollmentStatus.Active,
        };

    public Result<EnrollmentStatus> Transition(EnrollmentTrigger trigger, int userId, DateTime now, string? reason = null)
    {
        if (!Transitions.TryGetValue((EnrollmentStatus, trigger), out var target))
        {
            return Result<EnrollmentStatus>.Failure(GetWrongStateMessage(trigger));
        }

        if (trigger == EnrollmentTrigger.Reject && string.IsNullOrWhiteSpace(reason))
        {
            return Result<EnrollmentStatus>.Failure(EnrollmentRejectReasonRequiredMessage);
        }

        if (trigger == EnrollmentTrigger.Resume && !CanResume(now))
        {
            return Result<EnrollmentStatus>.Failure(EnrollmentResumeRequiresPaidTimeMessage);
        }

        EnrollmentStatus = target;

        if (trigger == EnrollmentTrigger.Approve)
        {
            DecidedById = userId;
            DecidedAt = now;
            DecisionNote = null;
        }
        else if (trigger == EnrollmentTrigger.Reject)
        {
            DecidedById = userId;
            DecidedAt = now;
            DecisionNote = reason!.Trim();
        }
        else if (trigger == EnrollmentTrigger.Request)
        {
            DecidedById = null;
            DecidedAt = null;
            DecisionNote = null;
        }

        return Result<EnrollmentStatus>.Success(EnrollmentStatus);
    }

    private static string GetWrongStateMessage(EnrollmentTrigger trigger) => trigger switch
    {
        EnrollmentTrigger.Request => EnrollmentAlreadyCompletedMessage,
        EnrollmentTrigger.Approve => EnrollmentApprovePendingOnlyMessage,
        EnrollmentTrigger.Reject => EnrollmentRejectPendingOnlyMessage,
        EnrollmentTrigger.Cancel => EnrollmentCancelPendingOrActiveOnlyMessage,
        EnrollmentTrigger.Complete => EnrollmentCompleteActiveOnlyMessage,
        EnrollmentTrigger.Resume => EnrollmentResumeCanceledOnlyMessage,
        _ => throw new ArgumentOutOfRangeException(nameof(trigger), trigger, null)
    };

    public bool CanResume(DateTime utcNow) =>
        EnrollmentStatus == EnrollmentStatus.Canceled && PaidUntil >= utcNow;

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

    /// <summary>
    /// A full refund of the payment whose period is the current access period moves PaidUntil back to that
    /// period's start. A partial refund, or a refund of a payment without an access period, changes nothing.
    /// </summary>
    public void RevokeRefundedAccessPeriod(CoursePayment payment)
    {
        if (payment.Status != PaymentStatus.Refunded || payment.PeriodStart is null || payment.PeriodEnd is null)
        {
            return;
        }

        if (PaidUntil == payment.PeriodEnd)
        {
            PaidUntil = payment.PeriodStart;
        }
    }

    /// <summary>
    /// A failed refund restores the access period that the counted refund had taken back.
    /// </summary>
    public void RestoreRefundedAccessPeriod(CoursePayment payment)
    {
        if (payment.Status == PaymentStatus.Refunded || payment.PeriodStart is null || payment.PeriodEnd is null)
        {
            return;
        }

        if (PaidUntil == payment.PeriodStart)
        {
            PaidUntil = payment.PeriodEnd;
        }
    }

    private const string EnrollmentAlreadyCompletedMessage = "Ovaj kurs ste već završili. Ponovni upis nije moguć.";
    private const string EnrollmentApprovePendingOnlyMessage = "Samo zahtjev za upis na čekanju može biti odobren.";
    private const string EnrollmentRejectPendingOnlyMessage = "Samo zahtjev za upis na čekanju se može odbiti.";
    private const string EnrollmentCancelPendingOrActiveOnlyMessage = "Otkazati se može samo zahtjev na čekanju ili aktivan upis.";
    private const string EnrollmentCompleteActiveOnlyMessage = "Samo aktivan upis može biti označen kao položen.";
    private const string EnrollmentResumeCanceledOnlyMessage = "Nastaviti se može samo otkazani upis.";
    private const string EnrollmentResumeRequiresPaidTimeMessage = "Pretplata se može nastaviti samo dok plaćeni period traje.";
    private const string EnrollmentRejectReasonRequiredMessage = "Razlog odbijanja je obavezan.";
}
