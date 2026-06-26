using eNote.Application.Common.Search;

namespace eNote.Application.Features.Academic.Assignments;

public class AssignmentSearchObject : BaseSearchObject
{
    public string? Title { get; set; }

    public DateTime? DueAfter { get; set; }
    public DateTime? DueBefore { get; set; }
}
