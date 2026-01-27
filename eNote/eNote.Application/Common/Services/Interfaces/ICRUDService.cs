using eNote.Application.Common.Search;

namespace eNote.Application.Common.Services.Interfaces
{
    public interface ICRUDService<TModel, TSearch, TInsert, TUpdate> : IService<TModel, TSearch> where TModel : class where TSearch : BaseSearchObject
    {
        Task<TModel> InsertAsync(TInsert request);
        Task<TModel> UpdateAsync(int id, TUpdate request);
        Task DeleteAsync(int id);
    }
}
