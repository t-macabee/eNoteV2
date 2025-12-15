namespace eNote.Domain.Entities
{
    public class LectureNote
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public DateTime CreatedAt { get; set; }

        public int LectureId { get; set; }
        public Lecture Lecture { get; set; } = null!;
    }
}
