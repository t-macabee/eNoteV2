using eNote.Application.SearchObjects;

namespace eNote.Application.Interfaces
{
    public interface ICRUDService<TModel, TSearch, TInsert, TUpdate> : IReadService<TModel, TSearch> where TModel : class where TSearch : BaseSearchObject
    {
        Task<TModel> InsertAsync(TInsert request);
        Task<TModel> UpdateAsync(int id, TUpdate request);
        Task DeleteAsync(int id);
    }
}
