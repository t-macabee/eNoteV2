using eNote.Application.Common.Paging;

namespace eNote.Application.Features.ReferenceData.InstrumentTypes;

public interface IInstrumentTypeService
{
    Task<PagedResult<InstrumentTypeDto>> GetPagedAsync(int page, int pageSize);
    Task<InstrumentTypeDto> GetByIdAsync(int id);
    Task<InstrumentTypeDto> CreateAsync(InstrumentTypeRequest request);
    Task<InstrumentTypeDto> UpdateAsync(int id, InstrumentTypeRequest request);
    Task DeleteAsync(int id);
}
