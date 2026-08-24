namespace eNote.Application.Constants;

public static class DbConstraintNames
{

    public const string InstrumentRentalActiveOrApprovedUniqueIndex = "UX_InstrumentRental_InstrumentId_ActiveOrApproved";

    public const string NotificationUserRentalCreatedAtUniqueIndex = "IX_Notification_UserId_RentalId_CreatedAt";

    public const string AssignmentSubmissionAssignmentIdStudentIdUniqueIndex = "UX_AssignmentSubmission_AssignmentId_StudentId";

    public const string NotificationUserLectureCreatedAtUniqueIndex = "IX_Notification_UserId_LectureId_CreatedAt";

    public const string NotificationUserSubmissionCreatedAtUniqueIndex = "IX_Notification_UserId_SubmissionId_CreatedAt";

    public const string RentalPaymentStripePaymentIntentIdUniqueIndex = "UX_RentalPayment_PaymentIntentId";

    public const string RentalPaymentStripeEventIdUniqueIndex = "UX_RentalPayment_StripeEventId";

    public const string StripeWebhookEventStripeEventIdUniqueIndex = "UX_StripeWebhookEvent_StripeEventId";
}
