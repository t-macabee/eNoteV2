namespace eNote.Application.Features.Rentals.ReferenceData.InstrumentTypes;

public interface IInstrumentTypeService
{
    Task<PagedResult<InstrumentTypeDto>> GetPagedAsync(InstrumentTypeSearchObject search, CancellationToken cancellationToken = default);
    Task<InstrumentTypeDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<InstrumentTypeDto> CreateAsync(InstrumentTypeRequest request, CancellationToken cancellationToken = default);
    Task<InstrumentTypeDto> UpdateAsync(int id, InstrumentTypeRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
