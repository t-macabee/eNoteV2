namespace eNote.Application.Features.Communication.Announcements.Services;

internal static class AnnouncementBuilder
{
    public static Announcement Build(AnnouncementRequest request, int? courseId, int? storeId, IClock clock) =>
        new(request.Title.Trim(), request.Content.Trim(), courseId, storeId, clock.UtcNow);
}
