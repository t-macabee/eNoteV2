using System.ComponentModel.DataAnnotations;

namespace eNote.Application.Features.Academic.Lectures;

public class LectureCreateRequest : LectureUpdateRequest
{
    public LectureType LectureType { get; set; }

    [Range(1, int.MaxValue)]
    public int CourseId { get; set; }
}
