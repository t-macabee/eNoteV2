using eNote.Application.Common.Search;

namespace eNote.Application.Features.Academic.Assignments;

public sealed class SubmissionSearchObject : BaseSearchObject
{
    public string? StudentName { get; set; }
    public bool? IsGraded { get; set; }
}
