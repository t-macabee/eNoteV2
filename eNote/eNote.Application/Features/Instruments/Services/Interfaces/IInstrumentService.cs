using eNote.Application.Common.Paging;
using eNote.Application.Features.Instruments.Search;

namespace eNote.Application.Features.Instruments.Services.Interfaces
{
    public interface IInstrumentService
    {
        Task<InstrumentDto> GetByIdAsync(int id, int employeeAppUserId);
        Task<InstrumentDto> GetPublicByIdAsync(int id);
        Task<PagedResult<InstrumentDto>> GetPagedAsync(InstrumentSearchObject search, int employeeAppUserId);
        Task<PagedResult<InstrumentDto>> GetPublicPagedAsync(InstrumentSearchObject search);
        Task<InstrumentDto> InsertAsync(InstrumentCreateRequest request, int employeeAppUserId);
        Task<InstrumentDto> UpdateAsync(int id, InstrumentUpdateRequest request, int employeeAppUserId);
        Task DeleteAsync(int id, int employeeAppUserId);
    }
}
