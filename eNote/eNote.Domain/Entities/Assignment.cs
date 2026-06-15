using eNote.Domain.Entities.Base;

namespace eNote.Domain.Entities
{
    public class Assignment : AuditableEntity
    {
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public DateTime DueAt { get; set; }
        public bool IsActive { get; set; } = true;

        public int LectureId { get; set; }
        public Lecture Lecture { get; set; } = null!;

        public ICollection<AssignmentSubmission> AssignmentSubmissions { get; set; } = new List<AssignmentSubmission>();
    }
}
