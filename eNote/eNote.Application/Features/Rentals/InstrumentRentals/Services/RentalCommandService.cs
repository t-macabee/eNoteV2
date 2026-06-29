using eNote.Domain.Entities;
using eNote.Domain.Shared;
using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Application.Features.Rentals.InstrumentRentals.Billing;
using eNote.Application.Features.Rentals.InstrumentRentals.StateMachine;
using eNote.Application.Features.Rentals.Instruments;
using eNote.Domain.Enums;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using eNote.Domain.Entities.Rentals;

namespace eNote.Application.Features.Rentals.InstrumentRentals.Services;

public sealed class RentalCommandService(IAppDbContext context, IMapper mapper, IClock clock, ICurrentActor actor, IRentalStateMachine stateMachine, IRentalNotificationDispatcher notificationDispatcher) : IRentalCommandService
{
    public async Task<InstrumentRentalDto> CreateRequestAsync(RentalCreateRequest request, CancellationToken cancellationToken = default)
    {
        var dto = await ExecuteInTransactionAsync(async () =>
        {
            var student = await actor.GetCurrentStudentAsync();

            if (!student.HasActiveMembership(clock.UtcNow))
            {
                throw new BusinessException(Messages.MembershipInactive);
            }

            var studentProfileId = student.Id;

            var instrument = await context.Set<Instrument>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == request.InstrumentId && x.IsActive, cancellationToken) ?? throw new NotFoundException(Messages.InstrumentNotFound);

            var alreadyPending = await context.Set<InstrumentRental>()
                .AnyAsync(x => x.InstrumentId == request.InstrumentId && x.StudentProfileId == studentProfileId && x.RentalStatus == InstrumentRentalStatus.Pending, cancellationToken);

            if (alreadyPending)
            {
                throw new BusinessException(Messages.RentalPendingRequired);
            }

            var rental = new InstrumentRental(request.InstrumentId, studentProfileId, instrument.MusicStoreId, clock.UtcNow, request.Note)
            {
                CreatedById = actor.UserId
            };

            context.Set<InstrumentRental>().Add(rental);
            await SaveWithLockConflictMessageAsync(Messages.InstrumentReservedOrRented, cancellationToken);

            var dto = await LoadDtoAsync(rental.Id, cancellationToken);
            await notificationDispatcher.DispatchCreatedAsync(dto, actor.UserId);
            await context.SaveChangesAsync(cancellationToken);

            return dto;
        }, cancellationToken);

        return dto;
    }

    public Task<InstrumentRentalDto> ApproveAsync(int rentalId, RentalStatusResponse response, CancellationToken cancellationToken = default) => ExecuteStoreTransitionAsync(rentalId, RentalTrigger.Approve, response, cancellationToken);

    public Task<InstrumentRentalDto> RejectAsync(int rentalId, RentalStatusResponse response, CancellationToken cancellationToken = default) => ExecuteStoreTransitionAsync(rentalId, RentalTrigger.Reject, response, cancellationToken);

    public Task<InstrumentRentalDto> PickupAsync(int rentalId, RentalStatusResponse response, CancellationToken cancellationToken = default) => ExecuteStoreTransitionAsync(rentalId, RentalTrigger.Pickup, response, cancellationToken);

    public Task<InstrumentRentalDto> CompleteAsync(int rentalId, RentalStatusResponse response, CancellationToken cancellationToken = default) => ExecuteStoreTransitionAsync(rentalId, RentalTrigger.Complete, response, cancellationToken);

    public Task<InstrumentRentalDto> ReturnEarlyAsync(int rentalId, RentalStatusResponse response, CancellationToken cancellationToken = default) => ExecuteStoreTransitionAsync(rentalId, RentalTrigger.ReturnEarly, response, cancellationToken);

    public Task<InstrumentRentalDto> CancelAsync(int rentalId, RentalStatusResponse response, CancellationToken cancellationToken = default) =>
        ExecuteInTransactionAsync(async () =>
        {
            var rental = await LoadForStudentAsync(rentalId, actor.UserId, cancellationToken);
            return await ExecuteTransitionWithNotificationAsync(rental, RentalTrigger.Cancel, RentalActor.Student, actor.UserId, response, cancellationToken);
        }, cancellationToken);

    private Task<InstrumentRentalDto> ExecuteStoreTransitionAsync(int rentalId, RentalTrigger trigger, RentalStatusResponse response, CancellationToken cancellationToken) =>
        ExecuteInTransactionAsync(async () =>
        {
            var storeId = await actor.GetCurrentStoreIdAsync(cancellationToken);
            var rental = await LoadForStoreAsync(rentalId, storeId, cancellationToken);
            return await ExecuteTransitionWithNotificationAsync(rental, trigger, RentalActor.StoreEmployee, actor.UserId, response, cancellationToken);
        }, cancellationToken);

    private async Task<InstrumentRentalDto> ExecuteTransitionWithNotificationAsync(InstrumentRental rental, RentalTrigger trigger, RentalActor rentalActor, int userId, RentalStatusResponse? response, CancellationToken cancellationToken)
    {
        var dto = await ExecuteTransitionAsync(rental, trigger, rentalActor, userId, response, cancellationToken);
        await notificationDispatcher.DispatchTransitionAsync(dto, trigger, userId);
        await context.SaveChangesAsync(cancellationToken); // saves the outbox row queued above; rental state was already saved inside ExecuteTransitionAsync, same transaction
        return dto;
    }

    private async Task<InstrumentRentalDto> ExecuteTransitionAsync(InstrumentRental rental, RentalTrigger trigger, RentalActor actor, int userId, RentalStatusResponse? response, CancellationToken cancellationToken)
    {
        var hasConflict = false;

        if (trigger is RentalTrigger.Approve or RentalTrigger.Pickup)
        {
            hasConflict = await context.Set<InstrumentRental>()
                .Where(x => x.InstrumentId == rental.InstrumentId && x.Id != rental.Id)
                .WhereBlockingStatus()
                .AnyAsync(cancellationToken);
        }

        var transitionContext = new RentalTransitionContext
        {
            UserId = userId,
            Actor = actor,
            HasInstrumentLockConflict = hasConflict,
            Response = response
        };

        var result = stateMachine.Fire(rental, trigger, transitionContext);

        if (!result.IsSuccess)
        {
            throw new BusinessException(result.Error);
        }

        if (result.Value.UsesInstrumentLock)
        {
            await SaveWithLockConflictMessageAsync(Messages.InstrumentReservedOrRented, cancellationToken);
        }
        else
        {
            await context.SaveChangesAsync(cancellationToken);
        }

        return await LoadDtoAsync(rental, cancellationToken);
    }

    private async Task<InstrumentRental> LoadForStoreAsync(int rentalId, int storeId, CancellationToken cancellationToken)
    {
        var rental = await context.Set<InstrumentRental>()
            .WithRentalDetails()
            .FirstOrDefaultAsync(x => x.Id == rentalId, cancellationToken) ?? throw new NotFoundException(Messages.RentalNotFound);

        if (rental.Instrument.MusicStoreId != storeId)
        {
            throw new BusinessException(Messages.RentalAccessDenied);
        }

        return rental;
    }

    private async Task<InstrumentRental> LoadForStudentAsync(int rentalId, int userId, CancellationToken cancellationToken)
    {
        var rental = await context.Set<InstrumentRental>()
            .WithRentalDetails()
            .FirstOrDefaultAsync(x => x.Id == rentalId, cancellationToken) ?? throw new NotFoundException(Messages.RentalNotFound);

        if (rental.StudentProfile.AppUserId != userId)
        {
            throw new BusinessException(Messages.RentalAccessDenied);
        }

        return rental;
    }

    // ponytail: no DB fetch — caller must pass an entity loaded via WithRentalDetails() or navigation fields map blank, not null
    private Task<InstrumentRentalDto> LoadDtoAsync(InstrumentRental entity, CancellationToken cancellationToken)
    {
        var result = mapper.Map<InstrumentRentalDto>(entity);
        RentalBilling.ApplyBilling(entity, result, clock.UtcNow);
        return Task.FromResult(result);
    }

    private async Task<InstrumentRentalDto> LoadDtoAsync(int rentalId, CancellationToken cancellationToken)
    {
        var entity = await context.Set<InstrumentRental>()
            .AsNoTracking()
            .WithRentalDetails()
            .FirstOrDefaultAsync(x => x.Id == rentalId, cancellationToken) ?? throw new NotFoundException(Messages.RentalNotFoundAfterUpdate);

        var result = mapper.Map<InstrumentRentalDto>(entity);
        RentalBilling.ApplyBilling(entity, result, clock.UtcNow);
        return result;
    }

    private async Task SaveWithLockConflictMessageAsync(string message, CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains("UX_InstrumentRental_InstrumentId_ActiveOrApproved") == true)
        {
            throw new BusinessException(message);
        }
    }

    private async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken)
    {
        using IDbContextTransaction transaction = await context.BeginTransactionAsync(cancellationToken);

        try
        {
            var result = await action();
            await transaction.CommitAsync(cancellationToken);

            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
