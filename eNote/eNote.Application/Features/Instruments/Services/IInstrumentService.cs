using eNote.Application.Common.Paging;

namespace eNote.Application.Features.Instruments.Services
{
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
}
