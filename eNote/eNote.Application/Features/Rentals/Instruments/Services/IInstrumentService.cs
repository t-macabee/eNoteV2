namespace eNote.Application.Features.Rentals.Instruments.Services;

public interface IInstrumentService
{
    Task<InstrumentDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<InstrumentDto> GetPublicByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PagedResult<InstrumentDto>> GetPagedAsync(InstrumentSearchObject search, CancellationToken cancellationToken = default);
    Task<PagedResult<InstrumentDto>> GetPublicPagedAsync(InstrumentSearchObject search, CancellationToken cancellationToken = default);
    Task<InstrumentDto> CreateAsync(InstrumentCreateRequest request, CancellationToken cancellationToken = default);
    Task<InstrumentDto> UpdateAsync(int id, InstrumentUpdateRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<InstrumentDto> UploadImageAsync(int id, Stream stream, string fileName, string contentType, CancellationToken ct = default);
}
