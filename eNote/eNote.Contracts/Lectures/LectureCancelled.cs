namespace eNote.Contracts.Lectures;

public record LectureCancelled(int LectureId, int StudentUserId, string LectureName, string Title, string Body, DateTime OccurredAtUtc);
