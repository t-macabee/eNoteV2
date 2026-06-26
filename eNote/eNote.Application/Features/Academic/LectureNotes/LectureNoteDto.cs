namespace eNote.Application.Features.Academic.LectureNotes;

public class LectureNoteDto
{
    public int Id { get; set; }
    public int LectureId { get; set; }

    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;
}
