using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Persistence;
using eNote.Application.Common.Queryable;
using eNote.Application.Common.Time;
using eNote.Application.Features.InstrumentRentals.Billing;
using eNote.Application.Features.InstrumentRentals.Services.Interfaces;
using eNote.Application.Features.InstrumentRentals.StateMachine;
using eNote.Application.Features.MusicStores.Services.Interfaces;
using eNote.Application.Features.Users;
using eNote.Domain.Entities;
using eNote.Domain.Enums;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.InstrumentRentals.Services
{
    public class RentalCommandService(IAppDbContext context, IMapper mapper, IClock clock, IMusicStoreContextService storeContext, IRentalStateMachine stateMachine) : IRentalCommandService
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

        public async Task<InstrumentRentalDto> ApproveAsync(int rentalId, int userId, RentalStatusResponse response) => await ExecuteStoreTransitionAsync(rentalId, userId, RentalTrigger.Approve, response);
        public async Task<InstrumentRentalDto> RejectAsync(int rentalId, int userId, RentalStatusResponse response) => await ExecuteStoreTransitionAsync(rentalId, userId, RentalTrigger.Reject, response);
        public async Task<InstrumentRentalDto> PickupAsync(int rentalId, int userId, RentalStatusResponse response) => await ExecuteStoreTransitionAsync(rentalId, userId, RentalTrigger.Pickup, response);
        public async Task<InstrumentRentalDto> CompleteAsync(int rentalId, int userId, RentalStatusResponse response) => await ExecuteStoreTransitionAsync(rentalId, userId, RentalTrigger.Complete, response);
        public async Task<InstrumentRentalDto> ReturnEarlyAsync(int rentalId, int userId, RentalStatusResponse response) => await ExecuteStoreTransitionAsync(rentalId, userId, RentalTrigger.ReturnEarly, response);

        public async Task<InstrumentRentalDto> CancelAsync(int rentalId, int userId, RentalStatusResponse response)
        {
            using var transaction = await context.BeginTransactionAsync();

            try
            {
                var rental = await LoadForStudentAsync(rentalId, userId);

                var result = await ExecuteTransitionAsync(rental, RentalTrigger.Cancel, RentalActor.Student, userId, response);

                await transaction.CommitAsync();

                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task<InstrumentRentalDto> ExecuteStoreTransitionAsync(int rentalId, int userId, RentalTrigger trigger, RentalStatusResponse response)
        {
            using var transaction = await context.BeginTransactionAsync();

            try
            {
                var storeId = await storeContext.GetActiveStoreAsync(userId);

                var rental = await LoadForStoreAsync(rentalId, storeId);

                var result = await ExecuteTransitionAsync(rental, trigger, RentalActor.StoreEmployee, userId, response);

                await transaction.CommitAsync();

                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task<InstrumentRentalDto> ExecuteTransitionAsync(InstrumentRental rental, RentalTrigger trigger, RentalActor actor, int userId, RentalStatusResponse? response)
        {
            var transitionContext = new RentalTransitionContext
            {
                UserId = userId,
                Actor = actor,
                Db = context,
                Response = response
            };

            var result = await stateMachine.FireAsync(rental, trigger, transitionContext);

            if (result.UsesInstrumentLock)
                await SaveWithLockConflictMessageAsync(Messages.InstrumentReservedOrRented);
            else
                await context.SaveChangesAsync();

            return await LoadDtoAsync(rental.Id);
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
