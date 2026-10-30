using RentalStatusChangedMessage = eNote.Contracts.Rentals.RentalStatusChanged;
using RentalRefundedMessage = eNote.Contracts.Rentals.RentalRefunded;

namespace eNote.Contracts.Communication;

public static class NotificationMessageTypes
{
    public const string RentalStatusChanged = nameof(RentalStatusChanged);
    public const string LectureCancelled = nameof(LectureCancelled);
    public const string SubmissionGraded = nameof(SubmissionGraded);
    public const string PaymentRefunded = nameof(PaymentRefunded);
    public const string AnnouncementPublished = nameof(AnnouncementPublished);
    public const string EnrollmentStatusChanged = nameof(EnrollmentStatusChanged);
    public const string TuitionPayment = nameof(TuitionPayment);
    public const string MembershipWarning = nameof(MembershipWarning);
    public const string PasswordResetRequested = nameof(PasswordResetRequested);

    private static readonly (Type MessageType, string Name)[] TypeNames =
    [
        (typeof(RentalStatusChangedMessage), RentalStatusChanged),
        (typeof(eNote.Contracts.Lectures.LectureCancelled), LectureCancelled),
        (typeof(eNote.Contracts.Assignments.SubmissionGraded), SubmissionGraded),
        (typeof(RentalRefundedMessage), PaymentRefunded),
        (typeof(eNote.Contracts.Communication.AnnouncementPublished), AnnouncementPublished),
        (typeof(eNote.Contracts.Enrollments.EnrollmentStatusChanged), EnrollmentStatusChanged),
        (typeof(eNote.Contracts.Enrollments.TuitionPaymentNotification), TuitionPayment),
        (typeof(eNote.Contracts.Identity.MembershipWarning), MembershipWarning),
        (typeof(eNote.Contracts.Identity.PasswordResetRequested), PasswordResetRequested)
    ];

    private static readonly Dictionary<Type, string> NamesByType = TypeNames.ToDictionary(x => x.MessageType, x => x.Name);
    private static readonly Dictionary<string, Type> TypesByName = TypeNames.ToDictionary(x => x.Name, x => x.MessageType);

    public static IReadOnlyList<(Type MessageType, string Name)> All => TypeNames;

    public static string NameFor(Type messageType) =>
        NamesByType.TryGetValue(messageType, out var name)
            ? name
            : throw new InvalidOperationException($"No notification message type is registered for '{messageType.FullName}'.");

    public static Type TypeFor(string name) =>
        TypesByName.TryGetValue(name, out var type)
            ? type
            : throw new InvalidOperationException($"Unknown notification outbox message type '{name}'.");
}
