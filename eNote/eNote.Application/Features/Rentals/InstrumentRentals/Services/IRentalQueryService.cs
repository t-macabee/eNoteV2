namespace eNote.Application.Features.Rentals.InstrumentRentals.Services;

public interface IRentalQueryService
{
    Task<InstrumentRentalDto> GetByIdForStudentAsync(int rentalId, CancellationToken cancellationToken = default);
    Task<InstrumentRentalDto> GetByIdForStoreAsync(int rentalId, CancellationToken cancellationToken = default);
    Task<PagedResult<InstrumentRentalDto>> GetPagedForStudentAsync(InstrumentRentalSearchObject searchObject, CancellationToken cancellationToken = default);
    Task<PagedResult<InstrumentRentalDto>> GetPagedForStoreAsync(InstrumentRentalSearchObject searchObject, CancellationToken cancellationToken = default);
}
