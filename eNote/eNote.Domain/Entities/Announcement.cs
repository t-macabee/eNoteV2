using eNote.Domain.Entities.Base;

namespace eNote.Domain.Entities
{
    public class Announcement : AuditableEntity
    {
        public string Title { get; private set; } = null!;
        public string Content { get; private set; } = null!;
        public DateTime PublishedAt { get; private set; }
        public bool IsActive { get; private set; } = true;

        public int? CourseId { get; private set; }
        public Course? Course { get; private set; }

        public int? MusicStoreId { get; private set; }
        public MusicStore? MusicStore { get; private set; }

        protected Announcement() { }

        public Announcement(string title, string content, int? courseId, int? musicStoreId, DateTime publishedAt)
        {
            Title = title;
            Content = content;
            CourseId = courseId;
            MusicStoreId = musicStoreId;
            PublishedAt = publishedAt;
            IsActive = true;
        }

        public void UpdateDetails(string title, string content)
        {
            Title = title;
            Content = content;
        }

        public void SoftDelete()
        {
            IsActive = false;
        }
    }
}
