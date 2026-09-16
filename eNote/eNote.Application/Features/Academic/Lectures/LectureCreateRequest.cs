namespace eNote.Application.Features.Academic.Lectures;

public class LectureCreateRequest : LectureUpdateRequest
{
    public LectureType LectureType { get; set; }

    public int CourseId { get; set; }
}
