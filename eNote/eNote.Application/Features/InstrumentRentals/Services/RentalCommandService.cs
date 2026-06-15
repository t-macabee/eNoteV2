using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
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
    public class RentalCommandService(IAppDbContext context, IMapper mapper, IClock clock, IMusicStoreContextService storeContext, IRentalStateMachine stateMachine, ICurrentUserService currentUserService) : IRentalCommandService
    {
        public async Task<InstrumentRentalDto> CreateRequestAsync(RentalCreateRequest request)
        {
            using var transaction = await context.BeginTransactionAsync();

            try
            {
                var studentProfileId = (await UserProfileHelper.GetStudentByUserIdAsync(context, currentUserService.UserId)).Id;

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
                    CreatedById = currentUserService.UserId
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

        public async Task<InstrumentRentalDto> ApproveAsync(int rentalId, RentalStatusResponse response) => await ExecuteStoreTransitionAsync(rentalId, RentalTrigger.Approve, response);
        public async Task<InstrumentRentalDto> RejectAsync(int rentalId, RentalStatusResponse response) => await ExecuteStoreTransitionAsync(rentalId, RentalTrigger.Reject, response);
        public async Task<InstrumentRentalDto> PickupAsync(int rentalId, RentalStatusResponse response) => await ExecuteStoreTransitionAsync(rentalId, RentalTrigger.Pickup, response);
        public async Task<InstrumentRentalDto> CompleteAsync(int rentalId, RentalStatusResponse response) => await ExecuteStoreTransitionAsync(rentalId, RentalTrigger.Complete, response);
        public async Task<InstrumentRentalDto> ReturnEarlyAsync(int rentalId, RentalStatusResponse response) => await ExecuteStoreTransitionAsync(rentalId, RentalTrigger.ReturnEarly, response);

        public async Task<InstrumentRentalDto> CancelAsync(int rentalId, RentalStatusResponse response)
        {
            using var transaction = await context.BeginTransactionAsync();

            try
            {
                var rental = await LoadForStudentAsync(rentalId, currentUserService.UserId);

                var result = await ExecuteTransitionAsync(rental, RentalTrigger.Cancel, RentalActor.Student, currentUserService.UserId, response);

                await transaction.CommitAsync();

                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task<InstrumentRentalDto> ExecuteStoreTransitionAsync(int rentalId, RentalTrigger trigger, RentalStatusResponse response)
        {
            using var transaction = await context.BeginTransactionAsync();

            try
            {
                var storeId = await storeContext.GetActiveStoreAsync(currentUserService.UserId);

                var rental = await LoadForStoreAsync(rentalId, storeId);

                var result = await ExecuteTransitionAsync(rental, trigger, RentalActor.StoreEmployee, currentUserService.UserId, response);

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