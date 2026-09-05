using eNote.Application.Common.Search;

namespace eNote.Application.Features.Identity.Employees;

public sealed class ShopEmployeeSearchObject : BaseSearchObject
{
    public string? Name { get; set; }
    public int? MusicStoreId { get; set; }
    public bool? IsActive { get; set; }
}
