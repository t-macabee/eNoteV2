using eNote.Application.Common.Search;

namespace eNote.Application.Features.Identity.Users;

public sealed class AdminUserSearchObject : BaseSearchObject
{
    public string? Name { get; set; }
    public string? Role { get; set; }
    public bool? IsActive { get; set; }
}
