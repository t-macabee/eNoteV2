using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Application.Features.Rentals.InstrumentRentals;
using eNote.Application.Features.Rentals.InstrumentRentals.Billing;
using eNote.Application.Features.Rentals.InstrumentRentals.StateMachine;
using eNote.Application.Features.Rentals.MusicStores.Services;
using eNote.Domain.Entities.Rentals;
using eNote.Domain.Enums;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace eNote.Application.Features.Rentals.InstrumentRentals.Services;

public sealed class RentalCommandService(IAppDbContext context, IMapper mapper, IClock clock, IUserContextResolver resolver, IMusicStoreContextService storeContext, IRentalStateMachine stateMachine, ICurrentUserService currentUserService, IRentalNotificationDispatcher notificationDispatcher) : IRentalCommandService
{
    public async Task<InstrumentRentalDto> CreateRequestAsync(RentalCreateRequest request)
    {
        var dto = await ExecuteInTransactionAsync(async () =>
        {
            var student = await resolver.GetStudentAsync(currentUserService.UserId);

            if (!student.HasActiveMembership(clock.UtcNow))
            {
                throw new BusinessException(Messages.MembershipInactive);
            }

            var studentProfileId = student.Id;

            _ = await context.Set<Instrument>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == request.InstrumentId && x.IsActive)
                ?? throw new NotFoundException(Messages.InstrumentNotFound);

            var locked = await context.Set<InstrumentRental>()
                .AnyAsync(x => x.InstrumentId == request.InstrumentId
                && InstrumentRentalStatusSets.Blocking.Contains(x.RentalStatus));

            if (locked)
            {
                throw new BusinessException(Messages.InstrumentReservedOrRented);
            }

            var alreadyPending = await context.Set<InstrumentRental>()
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

            var dto = await LoadDtoAsync(rental.Id);
            await notificationDispatcher.DispatchCreatedAsync(dto, currentUserService.UserId);

            return dto;
        });

        return dto;
    }

    public async Task<InstrumentRentalDto> ApproveAsync(int rentalId, RentalStatusResponse response) => await ExecuteStoreTransitionAsync(rentalId, RentalTrigger.Approve, response);

    public async Task<InstrumentRentalDto> RejectAsync(int rentalId, RentalStatusResponse response) => await ExecuteStoreTransitionAsync(rentalId, RentalTrigger.Reject, response);

    public async Task<InstrumentRentalDto> PickupAsync(int rentalId, RentalStatusResponse response) => await ExecuteStoreTransitionAsync(rentalId, RentalTrigger.Pickup, response);

    public async Task<InstrumentRentalDto> CompleteAsync(int rentalId, RentalStatusResponse response) => await ExecuteStoreTransitionAsync(rentalId, RentalTrigger.Complete, response);

    public async Task<InstrumentRentalDto> ReturnEarlyAsync(int rentalId, RentalStatusResponse response) => await ExecuteStoreTransitionAsync(rentalId, RentalTrigger.ReturnEarly, response);

    public async Task<InstrumentRentalDto> CancelAsync(int rentalId, RentalStatusResponse response)
    {
        var dto = await ExecuteInTransactionAsync(async () =>
        {
            var rental = await LoadForStudentAsync(rentalId, currentUserService.UserId);
            var dto = await ExecuteTransitionAsync(rental, RentalTrigger.Cancel, RentalActor.Student, currentUserService.UserId, response);

            await notificationDispatcher.DispatchTransitionAsync(dto, RentalTrigger.Cancel, currentUserService.UserId);

            return dto;
        });

        return dto;
    }

    private Task<InstrumentRentalDto> ExecuteStoreTransitionAsync(int rentalId, RentalTrigger trigger, RentalStatusResponse response) => ExecuteInTransactionAsync(async () =>
    {
        var storeId = await storeContext.GetActiveStoreAsync(currentUserService.UserId);
        var rental = await LoadForStoreAsync(rentalId, storeId);
        var dto = await ExecuteTransitionAsync(rental, trigger, RentalActor.StoreEmployee, currentUserService.UserId, response);

        await notificationDispatcher.DispatchTransitionAsync(dto, trigger, currentUserService.UserId);

        return dto;
    });

    private async Task<InstrumentRentalDto> ExecuteTransitionAsync(InstrumentRental rental, RentalTrigger trigger, RentalActor actor, int userId, RentalStatusResponse? response)
    {
        var hasConflict = await context.Set<InstrumentRental>()
            .AnyAsync(x => x.InstrumentId == rental.InstrumentId && x.Id != rental.Id && InstrumentRentalStatusSets.Blocking.Contains(x.RentalStatus));

        var transitionContext = new RentalTransitionContext
        {
            UserId = userId,
            Actor = actor,
            HasInstrumentLockConflict = hasConflict,
            Response = response
        };

        var result = stateMachine.Fire(rental, trigger, transitionContext);

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
        var rental = await context.Set<InstrumentRental>()
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
        var rental = await context.Set<InstrumentRental>()
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

    private async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action)
    {
        using IDbContextTransaction transaction = await context.BeginTransactionAsync();

        try
        {
            var result = await action();
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
