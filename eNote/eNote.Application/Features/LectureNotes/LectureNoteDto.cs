namespace eNote.Application.Features.LectureNotes
{
    public class LectureNoteDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public int LectureId { get; set; }
    }
}
