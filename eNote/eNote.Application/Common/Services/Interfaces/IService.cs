using eNote.Application.Common.Paging;
using eNote.Application.Common.Search;

namespace eNote.Application.Common.Services.Interfaces
{
    public interface IService<TModel, in TSearch> where TSearch : BaseSearchObject
    {
        Task<TModel> GetByIdAsync(int id);
        Task<PagedResult<TModel>> GetPagedAsync(TSearch search);
    }
}
