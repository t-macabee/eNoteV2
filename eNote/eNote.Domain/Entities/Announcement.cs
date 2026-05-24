using eNote.Domain.Entities.Base;

namespace eNote.Domain.Entities
{
    public class Announcement : BaseEntity
    {
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public DateTime PublishedAt { get; set; } = DateTime.UtcNow;

        public int CreatedById { get; set; }

        public int? CourseId { get; set; }
        public Course? Course { get; set; }

        public int? MusicStoreId { get; set; }
        public MusicStore? MusicStore { get; set; }
    }
}
