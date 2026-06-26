using eNote.Application.Common.Paging;
using eNote.Application.Features.Rentals.Instruments;

namespace eNote.Application.Features.Rentals.Instruments.Services;

public interface IInstrumentService
{
    Task<InstrumentDto> GetByIdAsync(int id);
    Task<InstrumentDto> GetPublicByIdAsync(int id);
    Task<PagedResult<InstrumentDto>> GetPagedAsync(InstrumentSearchObject search);
    Task<PagedResult<InstrumentDto>> GetPublicPagedAsync(InstrumentSearchObject search);
    Task<InstrumentDto> CreateAsync(InstrumentCreateRequest request);
    Task<InstrumentDto> UpdateAsync(int id, InstrumentUpdateRequest request);
    Task DeleteAsync(int id);
    Task<InstrumentDto> UploadImageAsync(int id, Stream stream, string fileName, string contentType, CancellationToken ct = default);
}
