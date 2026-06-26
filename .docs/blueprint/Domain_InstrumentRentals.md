# Bounded Context: InstrumentRentals
Total Files Contained: 23
---

## File: eNote\eNote.Domain\Enums\InstrumentRentalStatus.cs
```cs
namespace eNote.Domain.Enums;

public enum InstrumentRentalStatus
{
    Pending = 1,
    Approved = 2,
    Active = 3,
    Completed = 4,
    Rejected = 5,
    Canceled = 6,
    ReturnedEarly = 7
}
```

## File: eNote\eNote.Domain\Enums\InstrumentRentalStatusExtensions.cs
```cs
namespace eNote.Domain.Enums;

public static class InstrumentRentalStatusExtensions
{
    public static bool BlocksInstrument(this InstrumentRentalStatus status) =>
        status is InstrumentRentalStatus.Approved or InstrumentRentalStatus.Active;

    public static bool IsBillingEligible(this InstrumentRentalStatus status) =>
        status is InstrumentRentalStatus.Active
            or InstrumentRentalStatus.Completed
            or InstrumentRentalStatus.ReturnedEarly;
}
```

## File: eNote\eNote.Domain\Enums\InstrumentRentalStatusSets.cs
```cs
namespace eNote.Domain.Enums;

public static class InstrumentRentalStatusSets
{
    public static readonly InstrumentRentalStatus[] Blocking =
    [
        InstrumentRentalStatus.Approved,
        InstrumentRentalStatus.Active
    ];

    public static readonly InstrumentRentalStatus[] History =
    [
        InstrumentRentalStatus.Approved,
        InstrumentRentalStatus.Active,
        InstrumentRentalStatus.Completed,
        InstrumentRentalStatus.ReturnedEarly
    ];
}
```

## File: eNote\eNote.Application\Features\Rentals\InstrumentRentals\InstrumentRentalDto.cs
```cs
using eNote.Domain.Enums;

namespace eNote.Application.Features.Rentals.InstrumentRentals;

public class InstrumentRentalDto
{
    public int Id { get; set; }
    public int InstrumentId { get; set; }
    public int MusicStoreId { get; set; }
    public int StudentProfileId { get; set; }
    public int StudentUserId { get; set; }

    public string InstrumentModel { get; set; } = null!;
    public string InstrumentType { get; set; } = null!;
    public string StoreName { get; set; } = null!;
    public InstrumentRentalStatus RentalStatus { get; set; }
    public string? RequestNote { get; set; }
    public string? Note { get; set; }

    public DateTime RequestedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public DateTime? PickedUpAt { get; set; }
    public DateTime? ReturnedAt { get; set; }

    public int? ApprovedById { get; set; }
    public int? RejectedById { get; set; }

    public decimal Fee { get; set; }
    public decimal? DailyFee { get; set; }
    public int? MonthsCharged { get; set; }
    public int? DaysCharged { get; set; }
    public bool IsProrated { get; set; }
    public decimal? TotalFee { get; set; }
}

```

## File: eNote\eNote.Application\Features\Rentals\InstrumentRentals\InstrumentRentalMappingConfig.cs
```cs
using eNote.Application.Features.Rentals.InstrumentRentals.Billing;
using eNote.Domain.Entities.Rentals;
using Mapster;

namespace eNote.Application.Features.Rentals.InstrumentRentals;

public sealed class InstrumentRentalMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<InstrumentRental, InstrumentRentalDto>()
            .Map(x => x.InstrumentModel, x => x.Instrument.Model)
            .Map(x => x.InstrumentType, x => x.Instrument.InstrumentType.Type)
            .Map(x => x.MusicStoreId, x => x.Instrument.MusicStoreId)
            .Map(x => x.StoreName, x => x.Instrument.MusicStore.StoreName)
            .Map(x => x.StudentUserId, x => x.StudentProfile.AppUserId)
            .AfterMapping((src, dest) => RentalBilling.ApplyBilling(src, dest, DateTime.UtcNow));
    }
}

```

## File: eNote\eNote.Application\Features\Rentals\InstrumentRentals\InstrumentRentalSearchExtensions.cs
```cs
using eNote.Application.Common.Search;
using eNote.Domain.Entities.Rentals;

namespace eNote.Application.Features.Rentals.InstrumentRentals;

public static class InstrumentRentalSearchExtensions
{
    public static IQueryable<InstrumentRental> ApplySearch(this IQueryable<InstrumentRental> query, InstrumentRentalSearchObject search) =>
        query
            .WhereEqualsIf(search.InstrumentId, x => x.InstrumentId == search.InstrumentId!.Value)
            .WhereEqualsIf(search.RentalStatus, x => x.RentalStatus == search.RentalStatus!.Value);
}
```

## File: eNote\eNote.Application\Features\Rentals\InstrumentRentals\InstrumentRentalSearchObject.cs
```cs
using eNote.Application.Common.Search;
using eNote.Domain.Enums;

namespace eNote.Application.Features.Rentals.InstrumentRentals;

public class InstrumentRentalSearchObject : BaseSearchObject
{
    public int? InstrumentId { get; set; }
    public InstrumentRentalStatus? RentalStatus { get; set; }
}

```

## File: eNote\eNote.Application\Features\Rentals\InstrumentRentals\RentalCreateRequest.cs
```cs
using System.ComponentModel.DataAnnotations;

namespace eNote.Application.Features.Rentals.InstrumentRentals;

public class RentalCreateRequest
{
    [Required]
    [Range(1, int.MaxValue)]
    public int InstrumentId { get; set; }
    public string? Note { get; set; }
}

```

## File: eNote\eNote.Application\Features\Rentals\InstrumentRentals\RentalQueryableExtensions.cs
```cs
using eNote.Domain.Entities.Rentals;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.Rentals.InstrumentRentals;

public static class RentalQueryableExtensions
{
    public static IQueryable<InstrumentRental> WithRentalDetails(this IQueryable<InstrumentRental> query) =>
        query
            .Include(s => s.StudentProfile)
            .Include(r => r.Instrument).ThenInclude(i => i.InstrumentType)
            .Include(r => r.Instrument).ThenInclude(i => i.MusicStore);
}
```

## File: eNote\eNote.Application\Features\Rentals\InstrumentRentals\RentalStatusResponse.cs
```cs
namespace eNote.Application.Features.Rentals.InstrumentRentals;

public class RentalStatusResponse
{
    public string? Note { get; set; }
}

```

## File: eNote\eNote.Application\Features\Rentals\InstrumentRentals\Billing\RentalBilling.cs
```cs
﻿using eNote.Application.Features.Rentals.InstrumentRentals;
using eNote.Domain.Entities.Rentals;
using eNote.Domain.Enums;

namespace eNote.Application.Features.Rentals.InstrumentRentals.Billing;

public static class RentalBilling
{
    private const int DaysPerBillingCycle = 30;
    public static void ApplyBilling(InstrumentRental rental, InstrumentRentalDto dto, DateTime nowUtc)
    {
        dto.Fee = rental.Fee;

        var result = Calculate(rental.Fee, rental.PickedUpAt, rental.ReturnedAt, rental.RentalStatus, nowUtc);

        dto.MonthsCharged = result.MonthsCharged;
        dto.DaysCharged = result.DaysCharged;
        dto.DailyFee = result.DailyFee;
        dto.IsProrated = result.IsProrated;
        dto.TotalFee = result.TotalFee;
    }

    private static BillingResult Calculate(decimal fee, DateTime? pickedUpAt, DateTime? returnedAt, InstrumentRentalStatus status, DateTime nowUtc)
    {
        if (!pickedUpAt.HasValue)
        {
            return new BillingResult(null, null, null, null, false);
        }

        if (!status.IsBillingEligible())
        {
            return new BillingResult(null, null, null, null, false);
        }

        var start = pickedUpAt.Value;

        var end = returnedAt ?? nowUtc;

        if (end < start)
        {
            end = start;
        }

        var daysCharged = (int)Math.Ceiling((end - start).TotalDays);

        if (daysCharged < 1)
        {
            daysCharged = 1;
        }

        if (status == InstrumentRentalStatus.ReturnedEarly)
        {
            var dailyFee = fee / DaysPerBillingCycle;
            var prorated = daysCharged * dailyFee;
            var totalFee = prorated > fee ? fee : prorated;

            return new BillingResult(MonthsCharged: null, DaysCharged: daysCharged, DailyFee: decimal.Round(dailyFee, 2), TotalFee: decimal.Round(totalFee, 2), IsProrated: true);
        }

        var monthsCharged = (int)Math.Ceiling((end - start).TotalDays / DaysPerBillingCycle);

        if (monthsCharged < 1)
        {
            monthsCharged = 1;
        }

        return new BillingResult(MonthsCharged: monthsCharged, DaysCharged: null, DailyFee: null, TotalFee: monthsCharged * fee, IsProrated: false);
    }

    private readonly record struct BillingResult(int? MonthsCharged, int? DaysCharged, decimal? DailyFee, decimal? TotalFee, bool IsProrated);
}

```

## File: eNote\eNote.Application\Features\Rentals\InstrumentRentals\Services\IRentalCommandService.cs
```cs
using eNote.Application.Features.Rentals.InstrumentRentals;

namespace eNote.Application.Features.Rentals.InstrumentRentals.Services;

public interface IRentalCommandService
{
    Task<InstrumentRentalDto> CreateRequestAsync(RentalCreateRequest request);
    Task<InstrumentRentalDto> ApproveAsync(int rentalId, RentalStatusResponse response);
    Task<InstrumentRentalDto> RejectAsync(int rentalId, RentalStatusResponse response);
    Task<InstrumentRentalDto> PickupAsync(int rentalId, RentalStatusResponse response);
    Task<InstrumentRentalDto> CompleteAsync(int rentalId, RentalStatusResponse response);
    Task<InstrumentRentalDto> CancelAsync(int rentalId, RentalStatusResponse response);
    Task<InstrumentRentalDto> ReturnEarlyAsync(int rentalId, RentalStatusResponse response);
}

```

## File: eNote\eNote.Application\Features\Rentals\InstrumentRentals\Services\IRentalQueryService.cs
```cs
using eNote.Application.Common.Paging;
using eNote.Application.Features.Rentals.InstrumentRentals;

namespace eNote.Application.Features.Rentals.InstrumentRentals.Services;

public interface IRentalQueryService
{
    Task<InstrumentRentalDto> GetByIdForStudentAsync(int rentalId);
    Task<InstrumentRentalDto> GetByIdForStoreAsync(int rentalId);
    Task<PagedResult<InstrumentRentalDto>> GetPagedForStudentAsync(InstrumentRentalSearchObject searchObject);
    Task<PagedResult<InstrumentRentalDto>> GetPagedForStoreAsync(InstrumentRentalSearchObject searchObject);
}

```

## File: eNote\eNote.Application\Features\Rentals\InstrumentRentals\Services\RentalCommandService.cs
```cs
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
        var transitionContext = new RentalTransitionContext
        {
            UserId = userId,
            Actor = actor,
            Db = context,
            Response = response
        };

        var result = await stateMachine.FireAsync(rental, trigger, transitionContext);

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

```

## File: eNote\eNote.Application\Features\Rentals\InstrumentRentals\Services\RentalQueryService.cs
```cs
﻿using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Paging;
using eNote.Application.Common.Persistence;
using eNote.Application.Features.Rentals.InstrumentRentals;
using eNote.Application.Features.Rentals.MusicStores.Services;
using eNote.Domain.Entities.Rentals;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.Rentals.InstrumentRentals.Services;

public sealed class RentalQueryService(
    IAppDbContext context,
    IMapper mapper,
    IMusicStoreContextService storeContext,
    ICurrentUserService currentUserService) : IRentalQueryService
{
    public async Task<InstrumentRentalDto> GetByIdForStudentAsync(int rentalId) =>
        mapper.Map<InstrumentRentalDto>(await FindRentalAsync(context.Set<InstrumentRental>()
        .Where(x => x.Id == rentalId && x.StudentProfile.AppUserId == currentUserService.UserId)));

    public async Task<InstrumentRentalDto> GetByIdForStoreAsync(int rentalId)
    {
        var storeId = await storeContext.GetActiveStoreAsync(currentUserService.UserId);

        return mapper.Map<InstrumentRentalDto>(await FindRentalAsync(context.Set<InstrumentRental>()
                .Where(x => x.Id == rentalId && x.Instrument.MusicStoreId == storeId)));
    }

    public Task<PagedResult<InstrumentRentalDto>> GetPagedForStudentAsync(InstrumentRentalSearchObject search) =>
        GetPagedAsync(
            context.Set<InstrumentRental>()
                .Where(x => x.StudentProfile.AppUserId == currentUserService.UserId),
            search);

    public async Task<PagedResult<InstrumentRentalDto>> GetPagedForStoreAsync(InstrumentRentalSearchObject search)
    {
        var storeId = await storeContext.GetActiveStoreAsync(currentUserService.UserId);

        return await GetPagedAsync(
            context.Set<InstrumentRental>().Where(x => x.Instrument.MusicStoreId == storeId),
            search);
    }

    private async Task<PagedResult<InstrumentRentalDto>> GetPagedAsync(
        IQueryable<InstrumentRental> query,
        InstrumentRentalSearchObject search) =>
        await query
            .AsNoTracking()
            .WithRentalDetails()
            .ApplySearch(search)
            .OrderByDescending(x => x.RequestedAt)
            .ToPagedResultAsync(search, mapper.Map<InstrumentRentalDto>);

    private async Task<InstrumentRental> FindRentalAsync(IQueryable<InstrumentRental> query) =>
        await query.AsNoTracking().WithRentalDetails().FirstOrDefaultAsync()
        ?? throw new NotFoundException(Messages.NotFound);
}

```

## File: eNote\eNote.Application\Features\Rentals\InstrumentRentals\StateMachine\IRentalStateMachine.cs
```cs
using eNote.Domain.Entities.Rentals;

namespace eNote.Application.Features.Rentals.InstrumentRentals.StateMachine;

public interface IRentalStateMachine
{
    Task<RentalTransitionResult> FireAsync(InstrumentRental rental, RentalTrigger trigger, RentalTransitionContext context, CancellationToken cancellationToken = default);
}

```

## File: eNote\eNote.Application\Features\Rentals\InstrumentRentals\StateMachine\RentalActor.cs
```cs
namespace eNote.Application.Features.Rentals.InstrumentRentals.StateMachine;

public enum RentalActor
{
    StoreEmployee,
    Student
}

```

## File: eNote\eNote.Application\Features\Rentals\InstrumentRentals\StateMachine\RentalStateMachine.cs
```cs
﻿using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Time;
using eNote.Domain.Entities.Rentals;
using eNote.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.Rentals.InstrumentRentals.StateMachine;

public sealed class RentalStateMachine(IClock clock) : IRentalStateMachine
{
    private static readonly IReadOnlyList<TransitionDefinition> Transitions = CreateTransitions();

    public async Task<RentalTransitionResult> FireAsync(InstrumentRental rental, RentalTrigger trigger, RentalTransitionContext context, CancellationToken cancellationToken = default)
    {
        var transition = FindTransition(rental.RentalStatus, trigger, context.Actor) ?? throw new BusinessException(GetInvalidTransitionMessage(trigger, context.Actor));

        if (transition.GuardAsync is not null)
        {
            await transition.GuardAsync(rental, context, cancellationToken);
        }

        transition.Apply(rental, context, clock);

        return new RentalTransitionResult(transition.UsesInstrumentLock);
    }

    private static TransitionDefinition? FindTransition(InstrumentRentalStatus currentStatus, RentalTrigger trigger, RentalActor actor) =>
        Transitions.FirstOrDefault(t => t.From == currentStatus && t.Trigger == trigger && t.Actors.Contains(actor));

    private static string GetInvalidTransitionMessage(RentalTrigger trigger, RentalActor actor)
    {
        if (Transitions.Any(t => t.Trigger == trigger && t.Actors.Contains(actor)))
        {
            return GetWrongStateMessage(trigger);
        }

        return Messages.RentalAccessDenied;
    }

    private static string GetWrongStateMessage(RentalTrigger trigger) => trigger switch
    {
        RentalTrigger.Approve => Messages.RentalApprovePendingOnly,
        RentalTrigger.Reject => Messages.RentalRejectPendingOnly,
        RentalTrigger.Pickup => Messages.RentalPickupApprovedOnly,
        RentalTrigger.Complete => Messages.RentalCompleteActiveOnly,
        RentalTrigger.Cancel => Messages.RentalCancelPendingOrApprovedOnly,
        RentalTrigger.ReturnEarly => Messages.RentalEarlyReturnActiveOnly,
        _ => Messages.BadRequest
    };

    private static async Task GuardNoInstrumentLockConflictAsync(InstrumentRental rental, RentalTransitionContext context, CancellationToken cancellationToken)
    {
        var conflict = await context.Db.Set<InstrumentRental>()
            .AnyAsync(x =>
                x.InstrumentId == rental.InstrumentId &&
                x.Id != rental.Id &&
                InstrumentRentalStatusSets.Blocking.Contains(x.RentalStatus),
                cancellationToken);

        if (conflict)
        {
            throw new BusinessException(Messages.InstrumentReservedOrRented);
        }
    }

    private static void GuardInstrumentActive(InstrumentRental rental)
    {
        if (!rental.Instrument.IsActive)
        {
            throw new BusinessException(Messages.InstrumentInactive);
        }
    }

    private static void GuardNotPickedUp(InstrumentRental rental)
    {
        if (rental.PickedUpAt.HasValue)
        {
            throw new BusinessException(Messages.RentalCancelBlockedAfterPickup);
        }
    }

    private static void GuardNotReturned(InstrumentRental rental)
    {
        if (rental.ReturnedAt.HasValue)
        {
            throw new BusinessException(Messages.RentalAlreadyCompleted);
        }
    }

    private static void GuardPickedUp(InstrumentRental rental)
    {
        if (!rental.PickedUpAt.HasValue)
        {
            throw new BusinessException(Messages.RentalNotPickedUp);
        }
    }

    private static void ApplyAuditFields(InstrumentRental rental, RentalTransitionContext context, IClock time)
    {
        rental.UpdatedById = context.UserId;
    }

    private static IReadOnlyList<TransitionDefinition> CreateTransitions() =>
    [
        new(
            From: InstrumentRentalStatus.Pending,
            Trigger: RentalTrigger.Approve,
            Actors: [RentalActor.StoreEmployee],
            GuardAsync: async (rental, context, ct) =>
            {
                GuardInstrumentActive(rental);
                await GuardNoInstrumentLockConflictAsync(rental, context, ct);
            },
            Apply: (rental, context, time) =>
            {
                rental.Approve(rental.Instrument.InstrumentType.MonthlyFee, context.Response?.Note, time.UtcNow, context.UserId);
                ApplyAuditFields(rental, context, time);
            },
            UsesInstrumentLock: true),

        new(
            From: InstrumentRentalStatus.Pending,
            Trigger: RentalTrigger.Reject,
            Actors: [RentalActor.StoreEmployee],
            GuardAsync: null,
            Apply: (rental, context, time) =>
            {
                rental.Reject(time.UtcNow, context.Response?.Note, context.UserId);
                ApplyAuditFields(rental, context, time);
            },
            UsesInstrumentLock: false),

        new(
            From: InstrumentRentalStatus.Pending,
            Trigger: RentalTrigger.Cancel,
            Actors: [RentalActor.Student],
            GuardAsync: (rental, _, _) =>
            {
                GuardNotPickedUp(rental);
                return Task.CompletedTask;
            },
            Apply: (rental, context, time) =>
            {
                var note = !string.IsNullOrWhiteSpace(context.Response?.Note) ? context.Response.Note : rental.Note;
                rental.Cancel(time.UtcNow, note);
                ApplyAuditFields(rental, context, time);
            },
            UsesInstrumentLock: false),

        new(
            From: InstrumentRentalStatus.Approved,
            Trigger: RentalTrigger.Pickup,
            Actors: [RentalActor.StoreEmployee],
            GuardAsync: async (rental, context, ct) =>
            {
                GuardInstrumentActive(rental);

                if (rental.PickedUpAt.HasValue) { throw new BusinessException(Messages.RentalAlreadyPickedUp); } await GuardNoInstrumentLockConflictAsync(rental, context, ct);
            },
            Apply: (rental, context, time) =>
            {
                var note = !string.IsNullOrWhiteSpace(context.Response?.Note) ? context.Response.Note : rental.Note;
                rental.Pickup(time.UtcNow, note);
                ApplyAuditFields(rental, context, time);
            },
            UsesInstrumentLock: true),

        new(
            From: InstrumentRentalStatus.Approved,
            Trigger: RentalTrigger.Cancel,
            Actors: [RentalActor.Student],
            GuardAsync: (rental, _, _) =>
            {
                GuardNotPickedUp(rental);
                return Task.CompletedTask;
            },
            Apply: (rental, context, time) =>
            {
                var note = !string.IsNullOrWhiteSpace(context.Response?.Note) ? context.Response.Note : rental.Note;
                rental.Cancel(time.UtcNow, note);
                ApplyAuditFields(rental, context, time);
            },
            UsesInstrumentLock: false),

        new(
            From: InstrumentRentalStatus.Active,
            Trigger: RentalTrigger.Complete,
            Actors: [RentalActor.StoreEmployee],
            GuardAsync: (rental, _, _) =>
            {
                GuardNotReturned(rental);
                return Task.CompletedTask;
            },
            Apply: (rental, context, time) =>
            {
                var note = !string.IsNullOrWhiteSpace(context.Response?.Note) ? context.Response.Note : rental.Note;
                rental.Complete(time.UtcNow, note);
                ApplyAuditFields(rental, context, time);
            },
            UsesInstrumentLock: false),

        new(
            From: InstrumentRentalStatus.Active,
            Trigger: RentalTrigger.ReturnEarly,
            Actors: [RentalActor.StoreEmployee],
            GuardAsync: (rental, _, _) =>
            {
                GuardNotReturned(rental);
                GuardPickedUp(rental);
                return Task.CompletedTask;
            },
            Apply: (rental, context, time) =>
            {
                var note = !string.IsNullOrWhiteSpace(context.Response?.Note) ? context.Response.Note : rental.Note;
                rental.ReturnEarly(time.UtcNow, note);
                ApplyAuditFields(rental, context, time);
            },
            UsesInstrumentLock: false),
    ];

    private sealed record TransitionDefinition(
        InstrumentRentalStatus From, RentalTrigger Trigger, RentalActor[] Actors,
        Func<InstrumentRental, RentalTransitionContext, CancellationToken, Task>? GuardAsync, Action<InstrumentRental,
        RentalTransitionContext, IClock> Apply, bool UsesInstrumentLock
    );
}

```

## File: eNote\eNote.Application\Features\Rentals\InstrumentRentals\StateMachine\RentalTransitionContext.cs
```cs
using eNote.Application.Common.Persistence;
using eNote.Application.Features.Rentals.InstrumentRentals;

namespace eNote.Application.Features.Rentals.InstrumentRentals.StateMachine;

public sealed class RentalTransitionContext
{
    public required int UserId { get; init; }
    public required RentalActor Actor { get; init; }
    public required IAppDbContext Db { get; init; }

    public RentalStatusResponse? Response { get; init; }
}

```

## File: eNote\eNote.Application\Features\Rentals\InstrumentRentals\StateMachine\RentalTransitionResult.cs
```cs
namespace eNote.Application.Features.Rentals.InstrumentRentals.StateMachine;

public sealed record RentalTransitionResult(bool UsesInstrumentLock);

```

## File: eNote\eNote.Application\Features\Rentals\InstrumentRentals\StateMachine\RentalTrigger.cs
```cs
namespace eNote.Application.Features.Rentals.InstrumentRentals.StateMachine;

public enum RentalTrigger
{
    Approve,
    Reject,
    Pickup,
    Complete,
    Cancel,
    ReturnEarly
}

```

## File: eNote\eNote.API\Controllers\InstrumentRentals\StoreRentalController.cs
```cs
﻿using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Rentals.InstrumentRentals;
using eNote.Application.Features.Rentals.InstrumentRentals.Services;
using eNote.Application.Features.Reports.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.InstrumentRentals;

[Authorize(Roles = AppRoles.StoreEmployee)]
[Route("api/shop/rentals")]
public sealed class StoreRentalController(IRentalQueryService queryService, IRentalCommandService commandService, IReportService reportService) : CoreController
{
    [HttpGet("report")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRentalReport(CancellationToken cancellationToken)
    {
        var pdf = await reportService.GenerateStoreRentalSummaryPdfAsync(cancellationToken);
        return File(pdf, "application/pdf", "store-rentals.pdf");
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<InstrumentRentalDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<InstrumentRentalDto>>> GetPaged([FromQuery] InstrumentRentalSearchObject search)
    {
        var result = await queryService.GetPagedForStoreAsync(search);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(InstrumentRentalDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InstrumentRentalDto>> GetById(int id)
    {
        var dto = await queryService.GetByIdForStoreAsync(id);
        return Ok(dto);
    }

    [HttpPost("{id:int}/approve")]
    [ProducesResponseType(typeof(InstrumentRentalDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InstrumentRentalDto>> Approve(int id, [FromBody] RentalStatusResponse response)
    {
        var dto = await commandService.ApproveAsync(id, response);
        return Ok(dto);
    }

    [HttpPost("{id:int}/reject")]
    [ProducesResponseType(typeof(InstrumentRentalDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InstrumentRentalDto>> Reject(int id, [FromBody] RentalStatusResponse response)
    {
        var dto = await commandService.RejectAsync(id, response);
        return Ok(dto);
    }

    [HttpPost("{id:int}/pickup")]
    [ProducesResponseType(typeof(InstrumentRentalDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InstrumentRentalDto>> Pickup(int id, [FromBody] RentalStatusResponse response)
    {
        var dto = await commandService.PickupAsync(id, response);
        return Ok(dto);
    }

    [HttpPost("{id:int}/complete")]
    [ProducesResponseType(typeof(InstrumentRentalDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InstrumentRentalDto>> Complete(int id, [FromBody] RentalStatusResponse response)
    {
        var dto = await commandService.CompleteAsync(id, response);
        return Ok(dto);
    }

    [HttpPost("{id:int}/return-early")]
    [ProducesResponseType(typeof(InstrumentRentalDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InstrumentRentalDto>> ReturnEarly(int id, [FromBody] RentalStatusResponse response)
    {
        var dto = await commandService.ReturnEarlyAsync(id, response);
        return Ok(dto);
    }
}

```

## File: eNote\eNote.API\Controllers\InstrumentRentals\StudentRentalController.cs
```cs
﻿using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Rentals.InstrumentRentals;
using eNote.Application.Features.Rentals.InstrumentRentals.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.InstrumentRentals;

[Authorize(Roles = AppRoles.Student)]
[Route("api/student/rentals")]
public sealed class StudentRentalController(IRentalQueryService queryService, IRentalCommandService commandService) : CoreController
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<InstrumentRentalDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<InstrumentRentalDto>>> GetPaged([FromQuery] InstrumentRentalSearchObject search)
    {
        var result = await queryService.GetPagedForStudentAsync(search);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(InstrumentRentalDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InstrumentRentalDto>> GetById(int id)
    {
        var dto = await queryService.GetByIdForStudentAsync(id);
        return Ok(dto);
    }

    [HttpPost]
    [ProducesResponseType(typeof(InstrumentRentalDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<InstrumentRentalDto>> Create([FromBody] RentalCreateRequest request)
    {
        var dto = await commandService.CreateRequestAsync(request);
        return CreatedAtAction(nameof(GetById), new
        {
            id = dto.Id
        }, dto);
    }

    [HttpPost("{id:int}/cancel")]
    [ProducesResponseType(typeof(InstrumentRentalDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InstrumentRentalDto>> Cancel(int id, [FromBody] RentalStatusResponse response)
    {
        var dto = await commandService.CancelAsync(id, response);
        return Ok(dto);
    }
}

```

