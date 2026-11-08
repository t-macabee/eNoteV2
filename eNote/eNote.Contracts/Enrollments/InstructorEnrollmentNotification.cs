namespace eNote.Contracts.Enrollments;

public record InstructorEnrollmentNotification(int EnrollmentId, int CourseId, int InstructorUserId, string Title, string Body, DateTime OccurredAtUtc);
