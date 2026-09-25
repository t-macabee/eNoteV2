using eNote.Domain.Entities.Academic;
using eNote.Domain.Enums;

namespace eNote.Tests.Academic;

public sealed class EnrollmentTransitionTests
{
    private static readonly DateTime Now = new(2026, 6, 22, 12, 0, 0, DateTimeKind.Utc);
    private const int UserId = 42;

    private static readonly HashSet<(EnrollmentStatus From, EnrollmentTrigger Trigger)> AllowedPairs =
    [
        (EnrollmentStatus.Canceled, EnrollmentTrigger.Request),
        (EnrollmentStatus.Rejected, EnrollmentTrigger.Request),
        (EnrollmentStatus.Pending, EnrollmentTrigger.Approve),
        (EnrollmentStatus.Pending, EnrollmentTrigger.Reject),
        (EnrollmentStatus.Pending, EnrollmentTrigger.Cancel),
        (EnrollmentStatus.Active, EnrollmentTrigger.Cancel),
        (EnrollmentStatus.Active, EnrollmentTrigger.Complete),
    ];

    public static TheoryData<EnrollmentStatus, EnrollmentTrigger, EnrollmentStatus> AllowedTransitions => new()
    {
        { EnrollmentStatus.Canceled, EnrollmentTrigger.Request, EnrollmentStatus.Pending },
        { EnrollmentStatus.Rejected, EnrollmentTrigger.Request, EnrollmentStatus.Pending },
        { EnrollmentStatus.Pending, EnrollmentTrigger.Approve, EnrollmentStatus.Active },
        { EnrollmentStatus.Pending, EnrollmentTrigger.Reject, EnrollmentStatus.Rejected },
        { EnrollmentStatus.Pending, EnrollmentTrigger.Cancel, EnrollmentStatus.Canceled },
        { EnrollmentStatus.Active, EnrollmentTrigger.Cancel, EnrollmentStatus.Canceled },
        { EnrollmentStatus.Active, EnrollmentTrigger.Complete, EnrollmentStatus.Completed },
    };

    public static TheoryData<EnrollmentStatus, EnrollmentTrigger> DisallowedTransitions()
    {
        var data = new TheoryData<EnrollmentStatus, EnrollmentTrigger>();

        foreach (var from in Enum.GetValues<EnrollmentStatus>())
        {
            foreach (var trigger in Enum.GetValues<EnrollmentTrigger>())
            {
                if (!AllowedPairs.Contains((from, trigger)))
                {
                    data.Add(from, trigger);
                }
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(AllowedTransitions))]
    public void Transition_AppliesEveryAllowedRow(EnrollmentStatus from, EnrollmentTrigger trigger, EnrollmentStatus expected)
    {
        var enrollment = new Enrollment(1, 1, from);
        var reason = trigger == EnrollmentTrigger.Reject ? "Nema mjesta." : null;

        var result = enrollment.Transition(trigger, UserId, Now, reason);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
        Assert.Equal(expected, enrollment.EnrollmentStatus);
        Assert.Equal(UserId, enrollment.UpdatedById);
    }

    [Theory]
    [MemberData(nameof(DisallowedTransitions))]
    public void Transition_RejectsEveryOtherPair(EnrollmentStatus from, EnrollmentTrigger trigger)
    {
        var enrollment = new Enrollment(1, 1, from);

        var result = enrollment.Transition(trigger, UserId, Now, "Razlog");

        Assert.False(result.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(result.Error));
        Assert.Equal(from, enrollment.EnrollmentStatus);
        Assert.Null(enrollment.UpdatedById);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public void Reject_Fails_WithoutReason(string? reason)
    {
        var enrollment = new Enrollment(studentId: 1, courseId: 1, EnrollmentStatus.Pending);

        var result = enrollment.Transition(EnrollmentTrigger.Reject, UserId, Now, reason);

        Assert.False(result.IsSuccess);
        Assert.Equal("Razlog odbijanja je obavezan.", result.Error);
        Assert.Equal(EnrollmentStatus.Pending, enrollment.EnrollmentStatus);
    }

    [Fact]
    public void Approve_And_Reject_RecordDecision()
    {
        var approved = new Enrollment(studentId: 1, courseId: 1, EnrollmentStatus.Pending);

        approved.Transition(EnrollmentTrigger.Approve, UserId, Now);

        Assert.Equal(UserId, approved.DecidedById);
        Assert.Equal(Now, approved.DecidedAt);
        Assert.Null(approved.DecisionNote);

        var rejected = new Enrollment(studentId: 1, courseId: 1, EnrollmentStatus.Pending);

        rejected.Transition(EnrollmentTrigger.Reject, UserId, Now, "  Nema mjesta.  ");

        Assert.Equal(UserId, rejected.DecidedById);
        Assert.Equal(Now, rejected.DecidedAt);
        Assert.Equal("Nema mjesta.", rejected.DecisionNote);
    }

    [Fact]
    public void Complete_KeepsApprovalDecision()
    {
        var enrollment = new Enrollment(studentId: 1, courseId: 1, EnrollmentStatus.Pending);
        enrollment.Transition(EnrollmentTrigger.Approve, UserId, Now);

        enrollment.Transition(EnrollmentTrigger.Complete, 77, Now.AddHours(1));

        Assert.Equal(EnrollmentStatus.Completed, enrollment.EnrollmentStatus);
        Assert.Equal(UserId, enrollment.DecidedById);
        Assert.Equal(Now, enrollment.DecidedAt);
        Assert.Equal(77, enrollment.UpdatedById);
    }

    [Fact]
    public void Request_AfterReject_ClearsDecision()
    {
        var enrollment = new Enrollment(studentId: 1, courseId: 1, EnrollmentStatus.Pending);
        enrollment.Transition(EnrollmentTrigger.Reject, UserId, Now, "Popunjeno");

        enrollment.Transition(EnrollmentTrigger.Request, 77, Now.AddHours(1));

        Assert.Equal(EnrollmentStatus.Pending, enrollment.EnrollmentStatus);
        Assert.Null(enrollment.DecidedById);
        Assert.Null(enrollment.DecidedAt);
        Assert.Null(enrollment.DecisionNote);
    }
}
