using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Persistence;
using eNote.Application.Common.Queryable;
using eNote.Application.Common.Time;
using eNote.Application.Features.InstrumentRentals.Billing;
using eNote.Application.Features.InstrumentRentals.Services.Interfaces;
using eNote.Application.Features.MusicStores.Services.Interfaces;
using eNote.Application.Features.Users;
using eNote.Domain.Entities;
using eNote.Domain.Enums;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.InstrumentRentals.Services
{
    public class RentalCommandService(IAppDbContext context, IMapper mapper, IClock clock, IMusicStoreContextService storeContext) : IRentalCommandService
    {
        public async Task<InstrumentRentalDto> CreateRequestAsync(int userId, RentalCreateRequest request)
        {
            using var transaction = await context.BeginTransactionAsync();
            try
            {
                var studentProfileId = (await UserProfileHelper.GetStudentByUserIdAsync(context, userId)).Id;

                var instrument = await context.Set<Instrument>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == request.InstrumentId && x.IsActive)
                    ?? throw new NotFoundException(Messages.InstrumentNotFound);

                var locked = await context.Set<InstrumentRental>()
                    .AnyAsync(x => x.InstrumentId == request.InstrumentId &&
                        (x.RentalStatus == InstrumentRentalStatus.Approved ||
                         x.RentalStatus == InstrumentRentalStatus.Active));

                if (locked)
                    throw new BusinessException(Messages.InstrumentReservedOrRented);

                var alreadyPending = await context.Set<InstrumentRental>()
                    .AnyAsync(x => x.InstrumentId == request.InstrumentId
                        && x.StudentProfileId == studentProfileId
                        && x.RentalStatus == InstrumentRentalStatus.Pending);

                if (alreadyPending)
                    throw new BusinessException(Messages.RentalPendingRequired);

                var rental = new InstrumentRental
                {
                    InstrumentId = request.InstrumentId,
                    StudentProfileId = studentProfileId,
                    Note = request.Note,
                    RequestedAt = clock.UtcNow,
                    RentalStatus = InstrumentRentalStatus.Pending,
                    Fee = 0m,
                    ApprovedAt = null,
                    PickedUpAt = null,
                    ReturnedAt = null,
                    CreatedAt = clock.UtcNow,
                    CreatedById = userId
                };

                context.Set<InstrumentRental>().Add(rental);

                await context.SaveChangesAsync();

                await transaction.CommitAsync();

                return await LoadDtoAsync(rental.Id);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<InstrumentRentalDto> ApproveAsync(int rentalId, int userId, RentalStatusResponse response)
        {
            using var transaction = await context.BeginTransactionAsync();

            try
            {
                var result = await ProcessStoreActionAsync(rentalId, userId, validateAsync: async r =>
                {
                    if (r.RentalStatus != InstrumentRentalStatus.Pending)
                        throw new BusinessException(Messages.RentalApprovePendingOnly);

                    if (!r.Instrument.IsActive)
                        throw new BusinessException(Messages.InstrumentInactive);

                    var conflict = await context.Set<InstrumentRental>()
                        .AnyAsync(x => x.InstrumentId == r.InstrumentId && x.Id != r.Id &&
                            (x.RentalStatus == InstrumentRentalStatus.Approved ||
                             x.RentalStatus == InstrumentRentalStatus.Active));

                    if (conflict)
                        throw new BusinessException(Messages.InstrumentReservedOrRented);
                }, applyChanges: r =>
                {
                    r.Fee = r.Instrument.InstrumentType.MonthlyFee;
                    r.ApprovedAt = clock.UtcNow;
                    r.RentalStatus = InstrumentRentalStatus.Approved;
                    r.UpdatedAt = clock.UtcNow;
                    r.UpdatedById = userId;

                    ApplyNote(response, r);
                }, concurrencyMessage: Messages.InstrumentReservedOrRented);

                await transaction.CommitAsync();

                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<InstrumentRentalDto> RejectAsync(int rentalId, int userId, RentalStatusResponse response)
        {
            using var transaction = await context.BeginTransactionAsync();

            try
            {
                var result = await ProcessStoreActionAsync(rentalId, userId, validateAsync: async r =>
                {
                    if (r.RentalStatus != InstrumentRentalStatus.Pending)
                        throw new BusinessException(Messages.RentalRejectPendingOnly);
                }, applyChanges: r =>
                {
                    r.RentalStatus = InstrumentRentalStatus.Rejected;
                    r.UpdatedAt = clock.UtcNow;
                    r.UpdatedById = userId;
                    ApplyNote(response, r);
                }, concurrencyMessage: null);

                await transaction.CommitAsync();
                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<InstrumentRentalDto> PickupAsync(int rentalId, int userId, RentalStatusResponse response)
        {
            using var transaction = await context.BeginTransactionAsync();

            try
            {
                var result = await ProcessStoreActionAsync(rentalId, userId, validateAsync: async r =>
                {
                    RequireStatus(r, InstrumentRentalStatus.Approved, Messages.RentalPickupApprovedOnly);
                    RequireNotSet(r.PickedUpAt, Messages.RentalAlreadyPickedUp);

                    if (!r.Instrument.IsActive)
                        throw new BusinessException(Messages.InstrumentInactive);
                }, applyChanges: r =>
                {
                    r.PickedUpAt = clock.UtcNow;
                    r.RentalStatus = InstrumentRentalStatus.Active;
                    r.UpdatedAt = clock.UtcNow;
                    r.UpdatedById = userId;

                    ApplyNote(response, r);
                }, concurrencyMessage: Messages.InstrumentReservedOrRented);

                await transaction.CommitAsync();
                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<InstrumentRentalDto> CompleteAsync(int rentalId, int userId, RentalStatusResponse response)
        {
            using var transaction = await context.BeginTransactionAsync();

            try
            {
                var result = await ProcessStoreActionAsync(rentalId, userId,
                    validateAsync: async r =>
                    {
                        RequireStatus(r, InstrumentRentalStatus.Active, Messages.RentalCompleteActiveOnly);
                        RequireNotSet(r.ReturnedAt, Messages.RentalAlreadyCompleted);
                    },
                    applyChanges: r =>
                    {
                        r.ReturnedAt = clock.UtcNow;
                        r.RentalStatus = InstrumentRentalStatus.Completed;
                        r.UpdatedAt = clock.UtcNow;
                        r.UpdatedById = userId;

                        ApplyNote(response, r);
                    },
                    concurrencyMessage: null);

                await transaction.CommitAsync();
                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<InstrumentRentalDto> CancelAsync(int rentalId, int userId, RentalStatusResponse response)
        {
            using var transaction = await context.BeginTransactionAsync();

            try
            {
                var rental = await LoadForStudentAsync(rentalId, userId);

                if (rental.RentalStatus is not (InstrumentRentalStatus.Pending or InstrumentRentalStatus.Approved))
                    throw new BusinessException(Messages.RentalCancelPendingOrApprovedOnly);

                RequireNotSet(rental.PickedUpAt, Messages.RentalCancelBlockedAfterPickup);

                rental.RentalStatus = InstrumentRentalStatus.Canceled;

                rental.UpdatedAt = clock.UtcNow;

                rental.UpdatedById = userId;

                ApplyNote(response, rental);

                await context.SaveChangesAsync();

                await transaction.CommitAsync();

                return await LoadDtoAsync(rental.Id);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<InstrumentRentalDto> ReturnEarlyAsync(int rentalId, int userId, RentalStatusResponse response)
        {
            using var transaction = await context.BeginTransactionAsync();
            try
            {
                var result = await ProcessStoreActionAsync(rentalId, userId, validateAsync: async r =>
                {
                    RequireStatus(r, InstrumentRentalStatus.Active, Messages.RentalEarlyReturnActiveOnly);
                    RequireNotSet(r.ReturnedAt, Messages.RentalAlreadyCompleted);

                    if (!r.PickedUpAt.HasValue)
                        throw new BusinessException(Messages.RentalNotPickedUp);
                }, applyChanges: r =>
                {
                    r.ReturnedAt = clock.UtcNow;
                    r.RentalStatus = InstrumentRentalStatus.ReturnedEarly;
                    r.UpdatedAt = clock.UtcNow;
                    r.UpdatedById = userId;
                    ApplyNote(response, r);
                }, concurrencyMessage: null);

                await transaction.CommitAsync();
                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task<InstrumentRental> LoadForStoreAsync(int rentalId, int storeId)
        {
            var rental = await context.Set<InstrumentRental>()
                .WithRentalDetails()
                .FirstOrDefaultAsync(x => x.Id == rentalId)
                ?? throw new NotFoundException(Messages.RentalNotFound);

            if (rental.Instrument == null)
                throw new BusinessException(Messages.RentalInstrumentMissing);

            if (rental.Instrument.MusicStoreId != storeId)
                throw new BusinessException(Messages.RentalAccessDenied);

            return rental;
        }

        private async Task<InstrumentRental> LoadForStudentAsync(int rentalId, int userId)
        {
            var rental = await context.Set<InstrumentRental>()
                .WithRentalDetails()
                .FirstOrDefaultAsync(x => x.Id == rentalId)
                ?? throw new NotFoundException(Messages.RentalNotFound);

            if (rental.StudentProfile.AppUserId != userId)
                throw new BusinessException(Messages.RentalAccessDenied);

            return rental;
        }

        private async Task<InstrumentRentalDto> LoadDtoAsync(int rentalId)
        {
            var entity = await context.Set<InstrumentRental>()
                .AsNoTracking()
                .WithRentalDetails()
                .FirstOrDefaultAsync(x => x.Id == rentalId)
                ?? throw new NotFoundException(Messages.RentalNotFoundAfterUpdate);

            var result = mapper.Map<InstrumentRentalDto>(entity);

            RentalBilling.ApplyBilling(entity, result, clock.UtcNow);

            return result;
        }

        private async Task<InstrumentRentalDto> ProcessStoreActionAsync(int rentalId, int userId, Func<InstrumentRental, Task>? validateAsync, Action<InstrumentRental>? applyChanges, string? concurrencyMessage)
        {
            var storeId = await storeContext.GetActiveStoreAsync(userId);

            var rental = await LoadForStoreAsync(rentalId, storeId);

            if (validateAsync is not null)
                await validateAsync(rental);

            applyChanges?.Invoke(rental);

            if (!string.IsNullOrWhiteSpace(concurrencyMessage))
                await SaveWithLockConflictMessageAsync(concurrencyMessage);
            else
                await context.SaveChangesAsync();

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
                throw new BusinessException(message);
        }

        private static void RequireNotSet(DateTime? value, string message)
        {
            if (value.HasValue)
                throw new BusinessException(message);
        }

        private async Task SaveWithLockConflictMessageAsync(string message)
        {
            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                throw new BusinessException(message);
            }
        }
    }
}
