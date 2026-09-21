using eNote.Application.Constants;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Application.Features.Rentals.Instruments;
using MapsterMapper;

namespace eNote.Application.Features.Rentals.InstrumentRentals.Services;

public sealed class RentalCommandService(IAppDbContext context, IMapper mapper, IClock clock, ICurrentUserContext currentUser, IStudentContext students, IStoreContext stores, IRentalNotificationDispatcher notificationDispatcher, IStudentDisplayNameService displayNames)
{
    public async Task<InstrumentRentalDto> CreateRequestAsync(RentalCreateRequest request, CancellationToken cancellationToken = default)
    {
        var dto = await context.ExecuteInTransactionAsync(async () =>
        {
            var student = await students.GetCurrentStudentAsync(cancellationToken);

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

            var hasUnpaidDebt = await context.Set<InstrumentRental>()
                .IgnoreQueryFilters()
                .AnyAsync(x => x.StudentProfileId == studentProfileId
                    && (x.RentalStatus == InstrumentRentalStatus.Completed || x.RentalStatus == InstrumentRentalStatus.ReturnedEarly)
                    && !x.IsPaid, cancellationToken);

            if (hasUnpaidDebt)
            {
                throw new BusinessException(Messages.RentalUnpaidDebt);
            }

            var rental = new InstrumentRental(request.InstrumentId, studentProfileId, instrument.MusicStoreId, clock.UtcNow, request.Note)
            {
                CreatedById = currentUser.UserId
            };

            context.Set<InstrumentRental>().Add(rental);
            await SaveWithLockConflictMessageAsync(Messages.InstrumentReservedOrRented, cancellationToken);

            var dto = await LoadDtoAsync(rental.Id, cancellationToken);
            await notificationDispatcher.DispatchCreatedAsync(dto, currentUser.UserId);
            await context.SaveChangesAsync(cancellationToken);

            return dto;
        }, cancellationToken);

        return dto;
    }

    public Task<InstrumentRentalDto> ApproveAsync(int rentalId, RentalStatusRequest? request, CancellationToken cancellationToken = default) => ExecuteStoreTransitionAsync(rentalId, RentalTrigger.Approve, request, cancellationToken);

    public Task<InstrumentRentalDto> RejectAsync(int rentalId, RentalStatusRequest? request, CancellationToken cancellationToken = default) => ExecuteStoreTransitionAsync(rentalId, RentalTrigger.Reject, request, cancellationToken);

    public Task<InstrumentRentalDto> PickupAsync(int rentalId, RentalStatusRequest? request, CancellationToken cancellationToken = default) => ExecuteStoreTransitionAsync(rentalId, RentalTrigger.Pickup, request, cancellationToken);

    public Task<InstrumentRentalDto> CompleteAsync(int rentalId, RentalStatusRequest? request, CancellationToken cancellationToken = default) => ExecuteStoreTransitionAsync(rentalId, RentalTrigger.Complete, request, cancellationToken);

    public Task<InstrumentRentalDto> ReturnEarlyAsync(int rentalId, RentalStatusRequest? request, CancellationToken cancellationToken = default) => ExecuteStoreTransitionAsync(rentalId, RentalTrigger.ReturnEarly, request, cancellationToken);

    public Task<InstrumentRentalDto> CancelForStoreAsync(int rentalId, RentalStatusRequest? request, CancellationToken cancellationToken = default) =>
        ExecuteStoreTransitionAsync(rentalId, RentalTrigger.Cancel, request, cancellationToken);

    public Task<InstrumentRentalDto> CancelAsync(int rentalId, RentalStatusRequest? request, CancellationToken cancellationToken = default) =>
        context.ExecuteInTransactionAsync(async () =>
        {
            var rental = await LoadForStudentAsync(rentalId, currentUser.UserId, cancellationToken);
            return await ExecuteTransitionWithNotificationAsync(rental, RentalTrigger.Cancel, RentalActor.Student, currentUser.UserId, request, cancellationToken);
        }, cancellationToken);

    private Task<InstrumentRentalDto> ExecuteStoreTransitionAsync(int rentalId, RentalTrigger trigger, RentalStatusRequest? request, CancellationToken cancellationToken) =>
        context.ExecuteInTransactionAsync(async () =>
        {
            await stores.GetCurrentStoreIdAsync(cancellationToken);
            var rental = await LoadForStoreAsync(rentalId, cancellationToken);
            return await ExecuteTransitionWithNotificationAsync(rental, trigger, RentalActor.StoreEmployee, currentUser.UserId, request, cancellationToken);
        }, cancellationToken);

    private async Task<InstrumentRentalDto> ExecuteTransitionWithNotificationAsync(InstrumentRental rental, RentalTrigger trigger, RentalActor rentalActor, int userId, RentalStatusRequest? request, CancellationToken cancellationToken)
    {
        var hasConflict = false;

        if (trigger is RentalTrigger.Approve)
        {
            hasConflict = await context.Set<InstrumentRental>()
                .Where(x => x.InstrumentId == rental.InstrumentId && x.Id != rental.Id)
                .WhereBlockingStatus()
                .AnyAsync(cancellationToken);
        }

        var transitionContext = new RentalTransitionContext
        {
            UserId = userId,
            Actor = rentalActor,
            HasInstrumentLockConflict = hasConflict,
            MonthlyFee = rental.Instrument.InstrumentType.MonthlyFee,
            ResponseNote = request?.Note
        };

        var result = rental.Transition(trigger, transitionContext, clock.UtcNow);

        if (!result.IsSuccess)
        {
            throw new BusinessException(result.Error);
        }

        var dto = await LoadDtoAsync(rental, cancellationToken);
        await notificationDispatcher.DispatchTransitionAsync(dto, trigger, userId);

        if (result.Value.UsesInstrumentLock)
        {
            await SaveWithLockConflictMessageAsync(Messages.InstrumentReservedOrRented, cancellationToken);
        }
        else
        {
            await SaveWithLockConflictMessageAsync(Messages.Conflict, cancellationToken);
        }

        return dto;
    }

    private async Task<InstrumentRental> LoadForStoreAsync(int rentalId, CancellationToken cancellationToken)
    {
        return await context.Set<InstrumentRental>()
            .WithRentalDetails()
            .FirstOrDefaultAsync(x => x.Id == rentalId, cancellationToken) ?? throw new NotFoundException(Messages.RentalNotFound);
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

    private async Task<InstrumentRentalDto> LoadDtoAsync(InstrumentRental entity, CancellationToken cancellationToken)
    {
        var result = mapper.Map<InstrumentRentalDto>(entity);
        result.ApplyCharges(entity, entity.CalculateCharges(clock.UtcNow));
        result.StudentName = await displayNames.GetStudentDisplayNameAsync(entity.StudentProfile, cancellationToken);
        return result;
    }

    private async Task<InstrumentRentalDto> LoadDtoAsync(int rentalId, CancellationToken cancellationToken)
    {
        var entity = await context.Set<InstrumentRental>()
            .AsNoTracking()
            .WithRentalDetails()
            .FirstOrDefaultAsync(x => x.Id == rentalId, cancellationToken) ?? throw new NotFoundException(Messages.RentalNotFoundAfterUpdate);

        var result = mapper.Map<InstrumentRentalDto>(entity);
        result.ApplyCharges(entity, entity.CalculateCharges(clock.UtcNow));
        result.StudentName = await displayNames.GetStudentDisplayNameAsync(entity.StudentProfile, cancellationToken);
        return result;
    }

    private async Task SaveWithLockConflictMessageAsync(string message, CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException(Messages.ConcurrencyConflict);
        }
        catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex, DbConstraintNames.InstrumentRentalActiveOrApprovedUniqueIndex))
        {
            throw new BusinessException(message);
        }
        catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex, DbConstraintNames.InstrumentRentalPendingUniqueIndex))
        {
            throw new BusinessException(Messages.RentalPendingRequired);
        }
    }

}
