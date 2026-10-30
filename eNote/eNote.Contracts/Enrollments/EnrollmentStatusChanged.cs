namespace eNote.Contracts.Enrollments;

public record EnrollmentStatusChanged(int EnrollmentId, int CourseId, int StudentUserId, string Title, string Body, DateTime OccurredAtUtc);
