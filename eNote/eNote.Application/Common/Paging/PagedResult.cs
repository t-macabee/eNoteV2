namespace eNote.Application.Common.Paging
{
    public class PagedResult<T>
    {
        public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();

        public int Page { get; init; }
        public int PageSize { get; init; }
        public int Count => Items.Count;
        public int? TotalCount { get; init; }

        public int? TotalPages => TotalCount.HasValue && PageSize > 0
                ? (int)Math.Ceiling((double)TotalCount / PageSize) 
                : null;
    }
}
