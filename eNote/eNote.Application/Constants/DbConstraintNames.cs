namespace eNote.Application.Constants;

/// <summary>
/// Names of DB-level unique indexes/constraints that application code needs to recognize
/// by name (e.g. to translate a <see cref="Microsoft.EntityFrameworkCore.DbUpdateException"/>
/// into a friendly error). Kept as shared constants so a rename is a compile error instead of
/// a silently-broken string match.
/// </summary>
public static class DbConstraintNames
{
    /// <summary>
    /// Enforces at most one Approved/Active rental per instrument at a time.
    /// Defined in <c>InstrumentRentalConfig</c>.
    /// </summary>
    public const string InstrumentRentalActiveOrApprovedUniqueIndex = "UX_InstrumentRental_InstrumentId_ActiveOrApproved";

    /// <summary>
    /// Prevents duplicate rental-status notifications for the same user/rental/timestamp.
    /// Defined in the <c>AddNotificationUniqueIndex</c> migration (no fluent EF config counterpart).
    /// </summary>
    public const string NotificationUserRentalCreatedAtUniqueIndex = "IX_Notification_UserId_RentalId_CreatedAt";

    /// <summary>
    /// Enforces at most one submission per student per assignment.
    /// Defined in <c>AssignmentSubmissionConfig</c>.
    /// </summary>
    public const string AssignmentSubmissionAssignmentIdStudentIdUniqueIndex = "UX_AssignmentSubmission_AssignmentId_StudentId";

    /// <summary>
    /// Prevents duplicate lecture-cancellation notifications for the same user/lecture/timestamp
    /// (mirrors <see cref="NotificationUserRentalCreatedAtUniqueIndex"/>). Defined in the
    /// <c>AddLectureAndSubmissionNotificationLinks</c> migration.
    /// </summary>
    public const string NotificationUserLectureCreatedAtUniqueIndex = "IX_Notification_UserId_LectureId_CreatedAt";

    /// <summary>
    /// Prevents duplicate submission-graded notifications for the same user/submission/timestamp
    /// (mirrors <see cref="NotificationUserRentalCreatedAtUniqueIndex"/>). Defined in the
    /// <c>AddLectureAndSubmissionNotificationLinks</c> migration.
    /// </summary>
    public const string NotificationUserSubmissionCreatedAtUniqueIndex = "IX_Notification_UserId_SubmissionId_CreatedAt";
}
