using System.ComponentModel.DataAnnotations;

namespace eNote.Application.Features.Academic.LectureNotes;

public class LectureNoteRequest
{
    [Required]
    public string Title { get; set; } = null!;

    [Required]
    public string Content { get; set; } = null!;
}
