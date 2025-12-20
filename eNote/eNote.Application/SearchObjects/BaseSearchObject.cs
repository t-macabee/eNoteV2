namespace eNote.Application.SearchObjects
{
    public record BaseSearchObject
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public bool IncludeTotalCount { get; set; } = false;
    }
}
