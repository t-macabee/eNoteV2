using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Application.Features.InstrumentRentals.Billing;
using eNote.Application.Features.InstrumentRentals.StateMachine;
using eNote.Application.Features.MusicStores.Services;
using eNote.Application.Features.Users.Services;
using eNote.Domain.Entities;
using eNote.Domain.Enums;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace eNote.Application.Features.InstrumentRentals.Services
{
    public class RentalCommandService(IAppDbContext context, IMapper mapper, IClock clock, IUserContextResolver resolver, IMusicStoreContextService storeContext, IRentalStateMachine stateMachine, ICurrentUserService currentUserService, IRentalNotificationDispatcher notificationDispatcher) : IRentalCommandService
    {
        public async Task<InstrumentRentalDto> CreateRequestAsync(RentalCreateRequest request)
        {
            InstrumentRentalDto dto = await ExecuteInTransactionAsync(async () =>
            {
                Student student = await resolver.GetStudentAsync(currentUserService.UserId);

                if (!student.HasActiveMembership(clock.UtcNow))
                {
                    throw new BusinessException(Messages.MembershipInactive);
                }

                int studentProfileId = student.Id;

                _ = await context.Set<Instrument>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == request.InstrumentId && x.IsActive)
                    ?? throw new NotFoundException(Messages.InstrumentNotFound);

                bool locked = await context.Set<InstrumentRental>()
                    .AnyAsync(x => x.InstrumentId == request.InstrumentId &&
                        (x.RentalStatus == InstrumentRentalStatus.Approved ||
                         x.RentalStatus == InstrumentRentalStatus.Active));

                if (locked)
                {
                    throw new BusinessException(Messages.InstrumentReservedOrRented);
                }

                bool alreadyPending = await context.Set<InstrumentRental>()
                    .AnyAsync(x => x.InstrumentId == request.InstrumentId
                        && x.StudentProfileId == studentProfileId
                        && x.RentalStatus == InstrumentRentalStatus.Pending);

                if (alreadyPending)
                {
                    throw new BusinessException(Messages.RentalPendingRequired);
                }

                var rental = new InstrumentRental(request.InstrumentId, studentProfileId, clock.UtcNow, request.Note)
                {
                    CreatedById = currentUserService.UserId
                };

                context.Set<InstrumentRental>().Add(rental);
                await context.SaveChangesAsync();

                return await LoadDtoAsync(rental.Id);
            });

            await notificationDispatcher.DispatchCreatedAsync(dto, currentUserService.UserId);

            return dto;
        }

        public async Task<InstrumentRentalDto> ApproveAsync(int rentalId, RentalStatusResponse response) =>
            await PublishAfterTransitionAsync(await ExecuteStoreTransitionAsync(rentalId, RentalTrigger.Approve, response), RentalTrigger.Approve);

        public async Task<InstrumentRentalDto> RejectAsync(int rentalId, RentalStatusResponse response) =>
            await PublishAfterTransitionAsync(await ExecuteStoreTransitionAsync(rentalId, RentalTrigger.Reject, response), RentalTrigger.Reject);

        public async Task<InstrumentRentalDto> PickupAsync(int rentalId, RentalStatusResponse response) =>
            await PublishAfterTransitionAsync(await ExecuteStoreTransitionAsync(rentalId, RentalTrigger.Pickup, response), RentalTrigger.Pickup);

        public async Task<InstrumentRentalDto> CompleteAsync(int rentalId, RentalStatusResponse response) =>
            await PublishAfterTransitionAsync(await ExecuteStoreTransitionAsync(rentalId, RentalTrigger.Complete, response), RentalTrigger.Complete);

        public async Task<InstrumentRentalDto> ReturnEarlyAsync(int rentalId, RentalStatusResponse response) =>
            await PublishAfterTransitionAsync(await ExecuteStoreTransitionAsync(rentalId, RentalTrigger.ReturnEarly, response), RentalTrigger.ReturnEarly);

        public async Task<InstrumentRentalDto> CancelAsync(int rentalId, RentalStatusResponse response)
        {
            InstrumentRentalDto dto = await ExecuteInTransactionAsync(async () =>
            {
                InstrumentRental rental = await LoadForStudentAsync(rentalId, currentUserService.UserId);

                return await ExecuteTransitionAsync(rental, RentalTrigger.Cancel, RentalActor.Student, currentUserService.UserId, response);
            });

            await notificationDispatcher.DispatchTransitionAsync(dto, RentalTrigger.Cancel, currentUserService.UserId);

            return dto;
        }

        private async Task<InstrumentRentalDto> PublishAfterTransitionAsync(InstrumentRentalDto dto, RentalTrigger trigger)
        {
            await notificationDispatcher.DispatchTransitionAsync(dto, trigger, currentUserService.UserId);

            return dto;
        }

        private Task<InstrumentRentalDto> ExecuteStoreTransitionAsync(int rentalId, RentalTrigger trigger, RentalStatusResponse response) => ExecuteInTransactionAsync(async () =>
        {
            int storeId = await storeContext.GetActiveStoreAsync(currentUserService.UserId);

            InstrumentRental rental = await LoadForStoreAsync(rentalId, storeId);

            return await ExecuteTransitionAsync(rental, trigger, RentalActor.StoreEmployee, currentUserService.UserId, response);
        });

        private async Task<InstrumentRentalDto> ExecuteTransitionAsync(InstrumentRental rental, RentalTrigger trigger, RentalActor actor, int userId, RentalStatusResponse? response)
        {
            var transitionContext = new RentalTransitionContext
            {
                UserId = userId,
                Actor = actor,
                Db = context,
                Response = response
            };

            RentalTransitionResult result = await stateMachine.FireAsync(rental, trigger, transitionContext);

            if (result.UsesInstrumentLock)
            {
                await SaveWithLockConflictMessageAsync(Messages.InstrumentReservedOrRented);
            }
            else
            {
                await context.SaveChangesAsync();
            }

            return await LoadDtoAsync(rental.Id);
        }

        private async Task<InstrumentRental> LoadForStoreAsync(int rentalId, int storeId)
        {
            InstrumentRental rental = await context.Set<InstrumentRental>()
                .WithRentalDetails()
                .FirstOrDefaultAsync(x => x.Id == rentalId)
                ?? throw new NotFoundException(Messages.RentalNotFound);

            if (rental.Instrument.MusicStoreId != storeId)
            {
                throw new BusinessException(Messages.RentalAccessDenied);
            }

            return rental;
        }

        private async Task<InstrumentRental> LoadForStudentAsync(int rentalId, int userId)
        {
            InstrumentRental rental = await context.Set<InstrumentRental>()
                .WithRentalDetails()
                .FirstOrDefaultAsync(x => x.Id == rentalId)
                ?? throw new NotFoundException(Messages.RentalNotFound);

            if (rental.StudentProfile.AppUserId != userId)
            {
                throw new BusinessException(Messages.RentalAccessDenied);
            }

            return rental;
        }

        private async Task<InstrumentRentalDto> LoadDtoAsync(int rentalId)
        {
            InstrumentRental entity = await context.Set<InstrumentRental>()
                .AsNoTracking()
                .WithRentalDetails()
                .FirstOrDefaultAsync(x => x.Id == rentalId)
                ?? throw new NotFoundException(Messages.RentalNotFoundAfterUpdate);

            InstrumentRentalDto result = mapper.Map<InstrumentRentalDto>(entity);

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

        private async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action)
        {
            using IDbContextTransaction transaction = await context.BeginTransactionAsync();

            try
            {
                T? result = await action();
                await transaction.CommitAsync();

                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
