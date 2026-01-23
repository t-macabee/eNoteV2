using eNote.Application.Common.Paging;
using eNote.Application.SearchObjects;

namespace eNote.Application.Interfaces.Base
{
    public interface IService<TModel, in TSearch> where TSearch : BaseSearchObject
    {
        Task<TModel> GetByIdAsync(int id);
        Task<PagedResult<TModel>> GetPagedAsync(TSearch search);
    }
}
