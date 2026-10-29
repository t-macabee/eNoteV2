using eNote.Application.Common.Paging;

namespace eNote.Application.Common.Search;

public class BaseSearchObject
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = PagingLimits.DefaultPageSize;
    public bool IncludeTotalCount { get; set; } = true;
}
