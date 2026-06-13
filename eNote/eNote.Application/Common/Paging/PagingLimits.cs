namespace eNote.Application.Common.Paging
{
    public static class PagingLimits
    {
        public const int DefaultPageSize = 20;

        public const int MaxPageSize = 100;

        public static (int Page, int PageSize) Normalize(int page, int pageSize)
        {
            page = page < 1 ? 1 : page;

            pageSize = pageSize < 1 ? DefaultPageSize : pageSize;

            pageSize = pageSize > MaxPageSize ? MaxPageSize : pageSize;

            return (page, pageSize);
        }
    }
}
