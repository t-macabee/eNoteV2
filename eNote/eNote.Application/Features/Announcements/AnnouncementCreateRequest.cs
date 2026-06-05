using System.ComponentModel.DataAnnotations;

namespace eNote.Application.Features.Announcements
{
    public sealed record AnnouncementCreateRequest([property: Required] string Title, [property: Required] string Content);
}
