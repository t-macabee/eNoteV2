using eNote.Domain.Enums;
using eNote.Domain.Shared;

namespace eNote.Domain.Entities.Academic;

public class Enrollment : AuditableEntity
{
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

    // The only allowed status changes. The route and role pick the trigger, so there is no actor column
    // (unlike InstrumentRental): Request/Cancel are student actions, Approve/Reject/Complete are instructor actions.
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

        EnrollmentStatus = target;
        UpdatedById = userId;

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
        _ => throw new ArgumentOutOfRangeException(nameof(trigger), trigger, null)
    };

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

    // Domain-owned transition failure messages. eNote.Application.Common.Localization.Messages
    // (the app-wide message catalog) is an Application-layer type that Domain cannot reference
    // without inverting the project dependency (eNote.Domain has zero project references).
    private const string EnrollmentAlreadyCompletedMessage = "Ovaj kurs ste već završili. Ponovni upis nije moguć.";
    private const string EnrollmentApprovePendingOnlyMessage = "Samo zahtjev za upis na čekanju može biti odobren.";
    private const string EnrollmentRejectPendingOnlyMessage = "Samo zahtjev za upis na čekanju se može odbiti.";
    private const string EnrollmentCancelPendingOrActiveOnlyMessage = "Otkazati se može samo zahtjev na čekanju ili aktivan upis.";
    private const string EnrollmentCompleteActiveOnlyMessage = "Samo aktivan upis može biti označen kao položen.";
    private const string EnrollmentRejectReasonRequiredMessage = "Razlog odbijanja je obavezan.";
}
