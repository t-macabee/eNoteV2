using eNote.Application.Common.Search;

namespace eNote.Application.Features.Identity.Students;

public sealed class StudentSearchObject : BaseSearchObject
{
    public string? Name { get; set; }
    public bool? IsActive { get; set; }
}
