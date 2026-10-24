namespace eNote.Contracts.Enrollments;

public record TuitionPaymentNotification(int EnrollmentId, int StudentUserId, string Title, string Body, DateTime OccurredAtUtc);
