namespace eNote.Application.Features.Academic.Lectures;

public class LectureUpdateRequest
{
    public required string Name { get; set; }
    public required string Location { get; set; }
    public DateTime LectureTime { get; set; }
    public int Duration { get; set; }
    public int? Capacity { get; set; }
}
