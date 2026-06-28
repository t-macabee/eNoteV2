using eNote.Domain.Entities;

namespace eNote.Domain.Entities;

public class Announcement : AuditableEntity
{
    public int? CourseId { get; private set; }
    public Course? Course { get; private set; }
    public int? MusicStoreId { get; private set; }
    public MusicStore? MusicStore { get; private set; }

    public string Title { get; private set; } = null!;
    public string Content { get; private set; } = null!;
    public string? ImagePath { get; private set; }

    public DateTime PublishedAt { get; private set; }
    public bool IsActive { get; private set; } = true;

    protected Announcement()
    {
    }

    public Announcement(string title, string content, int? courseId, int? musicStoreId, DateTime publishedAt, string? imagePath = null)
    {
        Title = title;
        Content = content;
        ImagePath = imagePath;
        CourseId = courseId;
        MusicStoreId = musicStoreId;
        PublishedAt = publishedAt;
        IsActive = true;
    }

    public void UpdateDetails(string title, string content, string? imagePath = null)
    {
        Title = title;
        Content = content;

        if (imagePath is not null)
        {
            ImagePath = imagePath;
        }
    }

    public void SetImagePath(string? imagePath)
    {
        ImagePath = imagePath;
    }

    public void SoftDelete()
    {
        IsActive = false;
    }
}
