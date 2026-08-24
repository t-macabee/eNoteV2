namespace eNote.Contracts.Communication;

public static class NotificationMessageTypes
{
    public const string RentalStatusChanged = nameof(RentalStatusChanged);
    public const string LectureCancelled = nameof(LectureCancelled);
    public const string SubmissionGraded = nameof(SubmissionGraded);
    public const string PaymentRefunded = nameof(PaymentRefunded);
}
