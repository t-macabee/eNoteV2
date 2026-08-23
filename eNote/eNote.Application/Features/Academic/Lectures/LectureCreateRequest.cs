using System.ComponentModel.DataAnnotations;

namespace eNote.Application.Features.Academic.Lectures;

public class LectureCreateRequest
{
    public required string Name { get; set; }
    public required string Location { get; set; }
    public LectureType LectureType { get; set; }
    public DateTime LectureTime { get; set; }

    public int Duration { get; set; }
    public int? Capacity { get; set; }

    [Range(1, int.MaxValue)]
    public int CourseId { get; set; }
}
