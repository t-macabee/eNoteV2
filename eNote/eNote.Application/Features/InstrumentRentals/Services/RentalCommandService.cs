using eNote.Application.Common.Persistence;
using eNote.Application.Common.Queryable;
using eNote.Application.Common.Time;
using eNote.Application.Features.InstrumentRentals.DTOs;
using eNote.Application.Features.InstrumentRentals.Requests;
using eNote.Application.Features.InstrumentRentals.Services.Interfaces;
using eNote.Application.Features.MusicStores.Context.Services;
using eNote.Domain.Entities;
using eNote.Domain.Enums;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.InstrumentRentals.Services
{
    public class RentalCommandService(IAppDbContext context, IMapper mapper, IClock clock, IMusicStoreContextService storeContext) : IRentalCommandService
    {
        private readonly IAppDbContext _context = context;
        private readonly IMapper _mapper = mapper;
        private readonly IClock _clock = clock;
        private readonly IMusicStoreContextService _storeContext = storeContext;

        public async Task<InstrumentRentalDto> CreateRequestAsync(int userId, RentalCreateRequest request)
        {
            var studentProfileId = await _context.Students
                .Where(s => s.AppUserId == userId)
                .Select(s => s.Id)
                .SingleAsync();

            var instrument = await _context.Set<Instrument>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == request.InstrumentId)
                ?? throw new KeyNotFoundException("Instrument nije pronađen.");

            if (!instrument.IsActive)
                throw new InvalidOperationException("Instrument nije aktivan.");

            var locked = await _context.Set<InstrumentRental>().
                AnyAsync(x => x.InstrumentId == request.InstrumentId &&
                    (x.RentalStatus == InstrumentRentalStatus.Approved ||
                     x.RentalStatus == InstrumentRentalStatus.Active));

            if (locked)
                throw new InvalidOperationException("Instrument je rezervisan ili već iznajmljen.");

            var alreadyPending = await _context.Set<InstrumentRental>()
                .AnyAsync(x => x.InstrumentId == request.InstrumentId
                    && x.StudentProfileId == studentProfileId
                    && x.RentalStatus == InstrumentRentalStatus.Pending);

            if (alreadyPending)
                throw new InvalidOperationException("Već imate zahtjev na čekanju za ovaj instrument.");

            var rental = new InstrumentRental
            {
                InstrumentId = request.InstrumentId,
                StudentProfileId = studentProfileId,
                Note = request.Note,
                RequestedAt = _clock.UtcNow,
                RentalStatus = InstrumentRentalStatus.Pending,

                Fee = 0m,
                ApprovedAt = null,
                PickedUpAt = null,
                ReturnedAt = null
            };

            _context.Set<InstrumentRental>().Add(rental);

            await _context.SaveChangesAsync();
            return await LoadDtoAsync(rental.Id);
        }

        public async Task<InstrumentRentalDto> ApproveAsync(int rentalId, int userId, RentalStatusResponse response)
        {
            return await ProcessStoreActionAsync(rentalId, userId, validateAsync: async r =>
            {

                if (r.RentalStatus != InstrumentRentalStatus.Pending)
                    throw new InvalidOperationException("Samo zahtjev na čekanju može biti odobren.");

                if (!r.Instrument.IsActive)
                    throw new InvalidOperationException("Instrument nije aktivan.");

                var conflict = await _context.Set<InstrumentRental>()
                    .AnyAsync(x => x.InstrumentId == r.InstrumentId && x.Id != r.Id &&
                        (x.RentalStatus == InstrumentRentalStatus.Approved ||
                         x.RentalStatus == InstrumentRentalStatus.Active));

                if (conflict)
                    throw new InvalidOperationException("Instrument je već rezervisan ili iznajmljen.");
            }, applyChanges: r =>
            {

                r.Fee = r.Instrument.InstrumentType.MonthlyFee;
                r.ApprovedAt = _clock.UtcNow;
                r.RentalStatus = InstrumentRentalStatus.Approved;

                ApplyNote(response, r);
            }, concurrencyMessage: "Instrument je već rezervisan ili iznajmljen."
            );
        }

        public async Task<InstrumentRentalDto> RejectAsync(int rentalId, int userId, RentalStatusResponse response)
        {
            return await ProcessStoreActionAsync(rentalId, userId, validateAsync: async r =>
            {

                if (r.RentalStatus != InstrumentRentalStatus.Pending)
                    throw new InvalidOperationException("Samo zahtjev na čekanju se može odbiti.");
            }, applyChanges: r =>
            {
                r.RentalStatus = InstrumentRentalStatus.Rejected;
                ApplyNote(response, r);
            },
            concurrencyMessage: null);
        }

        public async Task<InstrumentRentalDto> PickupAsync(int rentalId, int userId, RentalStatusResponse response)
        {
            return await ProcessStoreActionAsync(rentalId, userId, validateAsync: async r =>
            {
                RequireStatus(r, InstrumentRentalStatus.Approved, "Samo odobren rental se može preuzeti.");
                RequireNotSet(r.PickedUpAt, "Instrument je već preuzet.");

                if (!r.Instrument.IsActive)
                    throw new InvalidOperationException("Instrument nije aktivan.");
            },
            applyChanges: r =>
            {
                r.PickedUpAt = _clock.UtcNow;
                r.RentalStatus = InstrumentRentalStatus.Active;

                ApplyNote(response, r);
            },
            concurrencyMessage: "Instrument je već rezervisan ili iznajmljen.");
        }

        public async Task<InstrumentRentalDto> CompleteAsync(int rentalId, int userId, RentalStatusResponse response)
        {
            return await ProcessStoreActionAsync(rentalId, userId,
                validateAsync: async r =>
                {
                    RequireStatus(r, InstrumentRentalStatus.Active, "Samo aktivno iznajmljivanje se može završiti.");
                    RequireNotSet(r.ReturnedAt, "Iznajmljivanje je već završeno.");
                },
                applyChanges: r =>
                {
                    r.ReturnedAt = _clock.UtcNow;
                    r.RentalStatus = InstrumentRentalStatus.Completed;

                    ApplyNote(response, r);
                },
                concurrencyMessage: null);
        }

        public async Task<InstrumentRentalDto> CancelAsync(int rentalId, int userId, RentalStatusResponse response)
        {
            var rental = await LoadForStudentAsync(rentalId, userId);

            if (rental.RentalStatus is not (InstrumentRentalStatus.Pending or InstrumentRentalStatus.Approved))
                throw new InvalidOperationException("Samo zahtjev na čekanju ili odobren zahtjev se može otkazati.");

            RequireNotSet(rental.PickedUpAt, "Instrument je već preuzet, otkazivanje nije moguće.");

            rental.RentalStatus = InstrumentRentalStatus.Canceled;

            ApplyNote(response, rental);

            await _context.SaveChangesAsync();
            return await LoadDtoAsync(rental.Id);
        }

        public async Task<InstrumentRentalDto> ReturnEarlyAsync(int rentalId, int userId, RentalStatusResponse response)
        {
            return await ProcessStoreActionAsync(rentalId, userId, validateAsync: async r =>
            {
                RequireStatus(r, InstrumentRentalStatus.Active, "Samo aktivno iznajmljivanje se može prijevremeno završiti.");
                RequireNotSet(r.ReturnedAt, "Rental je već završen.");

                if (!r.PickedUpAt.HasValue)
                    throw new InvalidOperationException("Instrument nije preuzet.");
            }, applyChanges: r =>
            {
                r.ReturnedAt = _clock.UtcNow;
                r.RentalStatus = InstrumentRentalStatus.ReturnedEarly;
                ApplyNote(response, r);
            },
                concurrencyMessage: null
            );
        }

        private async Task<InstrumentRental> LoadForStoreAsync(int rentalId, int storeId)
        {
            var rental = await _context.Set<InstrumentRental>()
                .WithRentalDetails()
                .FirstOrDefaultAsync(x => x.Id == rentalId)
                ?? throw new KeyNotFoundException("Zahtjev nije pronađen.");

            if (rental.Instrument == null)
                throw new InvalidOperationException("Instrument nije pronađen za ovaj zahtjev ");

            if (rental.Instrument.MusicStoreId != storeId)
                throw new InvalidOperationException("Nemate pravo nad ovim zahtjevom.");

            return rental;
        }

        private async Task<InstrumentRental> LoadForStudentAsync(int rentalId, int userId)
        {
            var rental = await _context.Set<InstrumentRental>()
                .WithRentalDetails()
                .FirstOrDefaultAsync(x => x.Id == rentalId)
                ?? throw new KeyNotFoundException("Zahtjev nije pronađen.");

            if (rental.StudentProfile.AppUserId != userId)
                throw new InvalidOperationException("Nemate pravo nad ovim zahtjevom.");

            return rental;
        }

        private async Task<InstrumentRentalDto> LoadDtoAsync(int rentalId)
        {
            var entity = await _context.Set<InstrumentRental>()
                .AsNoTracking()
                .WithRentalDetails()
                .FirstOrDefaultAsync(x => x.Id == rentalId)
                ?? throw new KeyNotFoundException("Zahtjev nije pronađen nakon ažuriranja.");

            var result = _mapper.Map<InstrumentRentalDto>(entity);

            RentalBilling.ApplyBilling(entity, result, _clock.UtcNow);

            return result;
        }

        private async Task<InstrumentRentalDto> ProcessStoreActionAsync(int rentalId, int userId, Func<InstrumentRental, Task>? validateAsync, Action<InstrumentRental>? applyChanges, string? concurrencyMessage)
        {
            var storeId = await _storeContext.GetActiveStoreAsync(userId);

            var rental = await LoadForStoreAsync(rentalId, storeId);

            if (validateAsync is not null)
                await validateAsync(rental);

            applyChanges?.Invoke(rental);

            if (!string.IsNullOrWhiteSpace(concurrencyMessage))
                await SaveWithLockConflictMessageAsync(concurrencyMessage);
            else
                await _context.SaveChangesAsync();

            return await LoadDtoAsync(rental.Id);
        }

        private static void ApplyNote(RentalStatusResponse? response, InstrumentRental rental)
        {
            if (!string.IsNullOrWhiteSpace(response?.Note))
                rental.Note = response.Note;
        }

        private static void RequireStatus(InstrumentRental rental, InstrumentRentalStatus expected, string message)
        {
            if (rental.RentalStatus != expected)
                throw new InvalidOperationException(message);
        }

        private static void RequireNotSet(DateTime? value, string message)
        {
            if (value.HasValue)
                throw new InvalidOperationException(message);
        }

        private async Task SaveWithLockConflictMessageAsync(string message)
        {
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
