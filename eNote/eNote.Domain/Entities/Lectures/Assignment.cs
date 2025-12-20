using eNote.Domain.Entities.Base;

namespace eNote.Domain.Entities.Lectures
{
    public class Assignment : BaseEntity
    {
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public DateTime DueAt { get; set; }

        public int LectureId { get; set; }
        public Lecture Lecture { get; set; } = null!;

        public ICollection<AssignmentSubmission> AssignmentSubmissions { get; set; } = new List<AssignmentSubmission>();
    }
}
