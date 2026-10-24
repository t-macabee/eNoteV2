namespace eNote.Application.Constants;

public static class DbConstraintNames
{

    public const string InstrumentRentalActiveOrApprovedUniqueIndex = "UX_InstrumentRental_InstrumentId_ActiveOrApproved";
    public const string InstrumentRentalPendingUniqueIndex = "UX_InstrumentRental_InstrumentId_StudentProfileId_Pending";

    public const string NotificationUserRentalCreatedAtUniqueIndex = "IX_Notification_UserId_RentalId_CreatedAt";

    public const string AssignmentSubmissionAssignmentIdStudentIdUniqueIndex = "UX_AssignmentSubmission_AssignmentId_StudentId";

    public const string NotificationUserLectureCreatedAtUniqueIndex = "IX_Notification_UserId_LectureId_CreatedAt";

    public const string NotificationUserSubmissionCreatedAtUniqueIndex = "IX_Notification_UserId_SubmissionId_CreatedAt";

    public const string NotificationUserAnnouncementCreatedAtUniqueIndex = "IX_Notification_UserId_AnnouncementId_CreatedAt";

    public const string NotificationUserEnrollmentCreatedAtUniqueIndex = "IX_Notification_UserId_EnrollmentId_CreatedAt";

    public const string RentalPaymentStripePaymentIntentIdUniqueIndex = "UX_RentalPayment_PaymentIntentId";

    public const string RentalPaymentOpenAttemptUniqueIndex = "UX_RentalPayment_InstrumentRentalId_OpenAttempt";

    public const string RentalPaymentStripeEventIdUniqueIndex = "UX_RentalPayment_StripeEventId";

    public const string CoursePaymentPaymentIntentIdUniqueIndex = "UX_CoursePayment_PaymentIntentId";

    public const string CoursePaymentOpenAttemptUniqueIndex = "UX_CoursePayment_EnrollmentId_OpenAttempt";

    public const string CoursePaymentStripeEventIdUniqueIndex = "UX_CoursePayment_StripeEventId";

    public const string PaymentRefundStripeRefundIdUniqueIndex = "UX_PaymentRefund_StripeRefundId";

    public const string PaymentRefundExactlyOnePaymentCheck = "CK_PaymentRefund_ExactlyOnePayment";

    public const string StripeWebhookEventStripeEventIdUniqueIndex = "UX_StripeWebhookEvent_StripeEventId";

    public const string RevokedTokenJtiUniqueIndex = "IX_RevokedToken_Jti";

    public const string InstrumentViewUserIdInstrumentIdUniqueIndex = "IX_InstrumentView_UserId_InstrumentId";

    public const string EnrollmentStudentIdCourseIdUniqueIndex = "IX_Enrollment_StudentId_CourseId";

    public const string AttendanceStudentIdLectureIdUniqueIndex = "IX_Attendance_StudentId_LectureId";
}
