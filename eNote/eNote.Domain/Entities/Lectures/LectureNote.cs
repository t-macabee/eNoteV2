using eNote.Domain.Entities.Base;

namespace eNote.Domain.Entities.Lectures
{
    public class LectureNote : BaseEntity
    {        
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public DateTime CreatedAt { get; set; }

        public int LectureId { get; set; }
        public Lecture Lecture { get; set; } = null!;
    }
}
