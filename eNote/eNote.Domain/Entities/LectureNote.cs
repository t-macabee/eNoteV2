using eNote.Domain.Entities.Base;

namespace eNote.Domain.Entities
{
    public class LectureNote : AuditableEntity
    {
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;

        public int LectureId { get; set; }
        public Lecture Lecture { get; set; } = null!;
    }
}
