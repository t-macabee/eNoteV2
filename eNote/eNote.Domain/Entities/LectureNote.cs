using eNote.Domain.Entities.Base;

namespace eNote.Domain.Entities
{
    public class LectureNote : AuditableEntity
    {
        public string Title { get; private set; } = null!;
        public string Content { get; private set; } = null!;

        public bool IsActive { get; private set; } = true;

        public int LectureId
        {
            get; private set;
        }
        public Lecture Lecture { get; private set; } = null!;

        protected LectureNote()
        {
        }

        public LectureNote(string title, string content, int lectureId)
        {
            Title = title;
            Content = content;
            LectureId = lectureId;
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
