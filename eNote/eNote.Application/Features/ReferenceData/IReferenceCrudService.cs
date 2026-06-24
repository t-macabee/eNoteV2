using eNote.Application.Common.Paging;
using eNote.Application.Common.Search;

namespace eNote.Application.Features.ReferenceData;

public interface IReferenceCrudService<TDto, TRequest, TSearch> where TSearch : BaseSearchObject
{
    Task<PagedResult<TDto>> GetPagedAsync(TSearch search);
    Task<TDto> GetByIdAsync(int id);
    Task<TDto> CreateAsync(TRequest request);
    Task<TDto> UpdateAsync(int id, TRequest request);
    Task DeleteAsync(int id);
}
