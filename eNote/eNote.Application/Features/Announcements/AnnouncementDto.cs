using eNote.Domain.Enums;

namespace eNote.Application.Features.Announcements
{
    public class AnnouncementDto
    {
        public int Id { get; set; }
        public int? CourseId { get; set; }
        public int? MusicStoreId { get; set; }

        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public string? ImagePath { get; set; }
        public AnnouncementScope Scope { get; set; }
        public string? CourseName { get; set; }
        public string? StoreName { get; set; }

        public DateTime PublishedAt { get; set; }
    }
}
