namespace eNote.Application.SearchObjects
{
    public class BaseSearchObject
    {
        public int Page { get; set; } = 0;
        public int PageSize { get; set; } = 20;
        public bool IncludeTotalCount { get; set; } = false;
    }
}
