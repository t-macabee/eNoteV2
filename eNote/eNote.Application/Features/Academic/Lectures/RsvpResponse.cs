namespace eNote.Application.Features.Academic.Lectures;

public class RsvpResponse
{
    public int LectureId { get; set; }
    public int StudentId { get; set; }

    public bool Confirmed { get; set; }
}
