using eNote.Application.Common.Persistence;
using eNote.Application.Common.Queryable;
using eNote.Application.Common.Time;
using eNote.Application.Features.InstrumentRentals.DTOs;
using eNote.Application.Features.InstrumentRentals.Requests;
using eNote.Application.Features.InstrumentRentals.Services.Interfaces;
using eNote.Domain.Entities;
using eNote.Domain.Enums;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.InstrumentRentals.Services
{
    public class RentalCommandService(IAppDbContext context, IMapper mapper, IClock clock) : IRentalCommandService
    {
        private readonly IAppDbContext _context = context;
        private readonly IMapper _mapper = mapper;        
        private readonly IClock _clock = clock;        

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
            var rental = await LoadForShopAsync(rentalId, userId);

            if (rental.RentalStatus != InstrumentRentalStatus.Pending)
                throw new InvalidOperationException("Samo zahtjev na čekanju može biti odobren.");

            if (!rental.Instrument.IsActive)
                throw new InvalidOperationException("Instrument nije aktivan.");

            var conflict = await _context.Set<InstrumentRental>()
                .AnyAsync(x => x.InstrumentId == rental.InstrumentId && x.Id != rental.Id &&
                    (x.RentalStatus == InstrumentRentalStatus.Approved || 
                     x.RentalStatus == InstrumentRentalStatus.Active));

            if (conflict)
                throw new InvalidOperationException("Instrument je već rezervisan ili iznajmljen.");

            rental.Fee = rental.Instrument.InstrumentType.MonthlyFee;
            rental.ApprovedAt = _clock.UtcNow;
            rental.RentalStatus = InstrumentRentalStatus.Approved;

            ApplyNote(response, rental);

            await SaveWithLockConflictMessageAsync("Instrument je već rezervisan ili iznajmljen.");
            return await LoadDtoAsync(rental.Id);
        }

        public async Task<InstrumentRentalDto> RejectAsync(int rentalId, int userId, RentalStatusResponse response)
        {
            var rental = await LoadForShopAsync(rentalId, userId);

            if (rental.RentalStatus != InstrumentRentalStatus.Pending)
                throw new InvalidOperationException("Samo zahtjev na čekanju se može odbiti.");

            rental.RentalStatus = InstrumentRentalStatus.Rejected;

            ApplyNote(response, rental);

            await _context.SaveChangesAsync();
            return await LoadDtoAsync(rental.Id);
        }

        public async Task<InstrumentRentalDto> PickupAsync(int rentalId, int userId, RentalStatusResponse response)
        {
            var rental = await LoadForShopAsync(rentalId, userId);

            RequireStatus(rental, InstrumentRentalStatus.Approved, "Samo odobren rental se može preuzeti.");                
            RequireNotSet(rental.PickedUpAt, "Instrument je već preuzet.");

            if (!rental.Instrument.IsActive)
                throw new InvalidOperationException("Instrument nije aktivan.");

            rental.PickedUpAt = _clock.UtcNow;
            rental.RentalStatus = InstrumentRentalStatus.Active;

            ApplyNote(response, rental);

            await SaveWithLockConflictMessageAsync("Instrument je već rezervisan ili iznajmljen.");
            return await LoadDtoAsync(rental.Id);
        }

        public async Task<InstrumentRentalDto> CompleteAsync(int rentalId, int userId, RentalStatusResponse response)
        {
            var rental = await LoadForShopAsync(rentalId, userId);

            RequireStatus(rental, InstrumentRentalStatus.Active, "Samo aktivno iznajmljivanje se može završiti.");
            RequireNotSet(rental.ReturnedAt, "Iznajmljivanje je već završeno.");          

            rental.ReturnedAt = _clock.UtcNow;
            rental.RentalStatus = InstrumentRentalStatus.Completed;

            ApplyNote(response, rental);

            await _context.SaveChangesAsync();
            return await LoadDtoAsync(rental.Id);
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
            var rental = await LoadForShopAsync(rentalId, userId);

            RequireStatus(rental, InstrumentRentalStatus.Active, "Samo aktivno iznajmljivanje se može prijevremeno završiti.");
            RequireNotSet(rental.ReturnedAt, "Rental je već završen.");

            if (!rental.PickedUpAt.HasValue)
                throw new InvalidOperationException("Instrument nije preuzet.");

            rental.ReturnedAt = _clock.UtcNow;
            rental.RentalStatus = InstrumentRentalStatus.ReturnedEarly;

            ApplyNote(response, rental);

            await _context.SaveChangesAsync();

            return await LoadDtoAsync(rental.Id);
        }

        private async Task<InstrumentRental> LoadForShopAsync(int rentalId, int shopUserId)
        {
            var rental = await _context.Set<InstrumentRental>()
                .WithRentalDetails()
                .FirstOrDefaultAsync(x => x.Id == rentalId)
                ?? throw new KeyNotFoundException("Zahtjev nije pronađen.");

            if (rental.Instrument?.MusicShop.AppUserId != shopUserId)
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
