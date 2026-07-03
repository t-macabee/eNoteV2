using eNote.Application.Common.Search;

namespace eNote.Application.Features.Academic.Lectures;

public class LectureSearchObject : BaseSearchObject
{
    public int? CourseId { get; set; }
    public string? Name { get; set; }
    public LectureType? LectureType { get; set; }

    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}
