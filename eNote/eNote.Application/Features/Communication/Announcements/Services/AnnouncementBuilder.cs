namespace eNote.Application.Features.Communication.Announcements.Services;

internal static class AnnouncementBuilder
{
    public static Announcement Build(AnnouncementRequest request, int? courseId, int? storeId, IClock clock, ICurrentUserContext currentUser) =>
        new(request.Title.Trim(), request.Content.Trim(), courseId, storeId, clock.UtcNow)
        {
            CreatedById = currentUser.UserId
        };
}
