namespace eNote.Application.Common.Paging;

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int? TotalCount { get; init; }

    public int Count => Items.Count;
}
