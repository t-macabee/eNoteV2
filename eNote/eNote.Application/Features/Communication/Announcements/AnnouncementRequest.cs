using System.ComponentModel.DataAnnotations;

namespace eNote.Application.Features.Communication.Announcements;

public sealed record AnnouncementRequest([property: Required] string Title, [property: Required] string Content);
