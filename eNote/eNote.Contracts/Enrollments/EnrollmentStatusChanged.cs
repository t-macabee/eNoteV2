namespace eNote.Contracts.Enrollments;

public record EnrollmentStatusChanged(int EnrollmentId, int StudentUserId, string Title, string Body, DateTime OccurredAtUtc);
