namespace eNote.Contracts.Communication;

public record AnnouncementPublished(int AnnouncementId, int StudentUserId, string Title, string Body, DateTime OccurredAtUtc);
