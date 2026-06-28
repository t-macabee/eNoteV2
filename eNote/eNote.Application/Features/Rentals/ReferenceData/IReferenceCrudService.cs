using eNote.Application.Common.Paging;
using eNote.Application.Common.Search;

namespace eNote.Application.Features.Rentals.ReferenceData;

public interface IReferenceCrudService<TDto, TRequest, TSearch> where TSearch : BaseSearchObject
{
    Task<PagedResult<TDto>> GetPagedAsync(TSearch search, CancellationToken cancellationToken = default);
    Task<TDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<TDto> CreateAsync(TRequest request, CancellationToken cancellationToken = default);
    Task<TDto> UpdateAsync(int id, TRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
