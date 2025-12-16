using eNote.Application.SearchObjects;

namespace eNote.Application.Interfaces.Base
{
    public interface IReadService<TModel, in TSearch> where TSearch : BaseSearchObject
    {
        Task<TModel> GetByIdAsync(int id);
        Task<PagedResult<TModel>> GetPagedAsync(TSearch search);
    }
}
