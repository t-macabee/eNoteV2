namespace eNote.Application
{
    public class PagedResult<T>
    {
        public int Count { get; set; }              
        public int ReturnedCount { get; set; }      
        public List<T> ResultList { get; set; } = [];
    }
}
