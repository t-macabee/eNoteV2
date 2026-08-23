namespace eNote.Contracts.Assignments;

public record SubmissionGraded(int SubmissionId, int StudentUserId, string AssignmentTitle, int Grade, string Title, string Body, DateTime OccurredAtUtc);
