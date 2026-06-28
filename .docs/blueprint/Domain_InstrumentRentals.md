# Bounded Context: InstrumentRentals

**Generated**: 2026-06-28T16:22:41.265951+00:00  
**Commit**: latest  
**Total Files**: 23

---

## 🤖 Agent Briefing (Read First)

This file contains the complete source for the **InstrumentRentals** bounded context.

**Your goals when reading this context:**
1. Build an accurate mental model of entities, behavior, and state transitions.
2. Identify cross-context interactions (see "Key Interactions" sections).
3. Note any architectural smells, duplicated logic, or unnecessary abstractions.
4. Track how this context communicates with others (especially via events).

**Focus areas for deep analysis:**
- Domain entities with rich behavior (not anemic)
- Service orchestration and access control
- State machines / workflow logic
- Cross-domain event contracts

---

## File: `eNote\eNote.API\Controllers\InstrumentRentals\StoreRentalController.cs`
**Hash**: `7bb057460437` | **Size**: 3694 chars

**Classes**: StoreRentalController
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
    public async Task<ActionResult<PagedResult<InstrumentRentalDto>>> GetPaged([FromQuery] InstrumentRentalSearchObject search, CancellationToken cancellationToken)
    {
        var result = await queryService.GetPagedForStoreAsync(search, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(InstrumentRentalDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InstrumentRentalDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var dto = await queryService.GetByIdForStoreAsync(id, cancellationToken);
        return Ok(dto);
    }

    [HttpPost("{id:int}/approve")]
    [ProducesResponseType(typeof(InstrumentRentalDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InstrumentRentalDto>> Approve(int id, [FromBody] RentalStatusResponse response, CancellationToken cancellationToken)
    {
        var dto = await commandService.ApproveAsync(id, response, cancellationToken);
        return Ok(dto);
    }

    [HttpPost("{id:int}/reject")]
    [ProducesResponseType(typeof(InstrumentRentalDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InstrumentRentalDto>> Reject(int id, [FromBody] RentalStatusResponse response, CancellationToken cancellationToken)
    {
        var dto = await commandService.RejectAsync(id, response, cancellationToken);
        return Ok(dto);
    }

    [HttpPost("{id:int}/pickup")]
    [ProducesResponseType(typeof(InstrumentRentalDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InstrumentRentalDto>> Pickup(int id, [FromBody] RentalStatusResponse response, CancellationToken cancellationToken)
    {
        var dto = await commandService.PickupAsync(id, response, cancellationToken);
        return Ok(dto);
    }

    [HttpPost("{id:int}/complete")]
    [ProducesResponseType(typeof(InstrumentRentalDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InstrumentRentalDto>> Complete(int id, [FromBody] RentalStatusResponse response, CancellationToken cancellationToken)
    {
        var dto = await commandService.CompleteAsync(id, response, cancellationToken);
        return Ok(dto);
    }

    [HttpPost("{id:int}/return-early")]
    [ProducesResponseType(typeof(InstrumentRentalDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InstrumentRentalDto>> ReturnEarly(int id, [FromBody] RentalStatusResponse response, CancellationToken cancellationToken)
    {
        var dto = await commandService.ReturnEarlyAsync(id, response, cancellationToken);
        return Ok(dto);
    }
}

```

---

## File: `eNote\eNote.API\Controllers\InstrumentRentals\StudentRentalController.cs`
**Hash**: `b7c5f43b4e58` | **Size**: 2146 chars

**Classes**: StudentRentalController
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
    public async Task<ActionResult<PagedResult<InstrumentRentalDto>>> GetPaged([FromQuery] InstrumentRentalSearchObject search, CancellationToken cancellationToken)
    {
        var result = await queryService.GetPagedForStudentAsync(search, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(InstrumentRentalDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InstrumentRentalDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var dto = await queryService.GetByIdForStudentAsync(id, cancellationToken);
        return Ok(dto);
    }

    [HttpPost]
    [ProducesResponseType(typeof(InstrumentRentalDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<InstrumentRentalDto>> Create([FromBody] RentalCreateRequest request, CancellationToken cancellationToken)
    {
        var dto = await commandService.CreateRequestAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new
        {
            id = dto.Id
        }, dto);
    }

    [HttpPost("{id:int}/cancel")]
    [ProducesResponseType(typeof(InstrumentRentalDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InstrumentRentalDto>> Cancel(int id, [FromBody] RentalStatusResponse response, CancellationToken cancellationToken)
    {
        var dto = await commandService.CancelAsync(id, response, cancellationToken);
        return Ok(dto);
    }
}

```

---

## File: `eNote\eNote.Application\Features\Rentals\InstrumentRentals\Billing\RentalBilling.cs`
**Hash**: `129de86dac14` | **Size**: 2315 chars

**Classes**: RentalBilling
```cs
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

---

## File: `eNote\eNote.Application\Features\Rentals\InstrumentRentals\InstrumentRentalDto.cs`
**Hash**: `d17495dd1692` | **Size**: 1212 chars

**Classes**: InstrumentRentalDto
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

---

## File: `eNote\eNote.Application\Features\Rentals\InstrumentRentals\InstrumentRentalMappingConfig.cs`
**Hash**: `c54521003ed0` | **Size**: 676 chars

**Classes**: InstrumentRentalMappingConfig
```cs
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
            .Map(x => x.StudentUserId, x => x.StudentProfile.AppUserId);
    }
}

```

---

## File: `eNote\eNote.Application\Features\Rentals\InstrumentRentals\InstrumentRentalSearchExtensions.cs`
**Hash**: `8acf6455d915` | **Size**: 551 chars

**Classes**: InstrumentRentalSearchExtensions
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

---

## File: `eNote\eNote.Application\Features\Rentals\InstrumentRentals\InstrumentRentalSearchObject.cs`
**Hash**: `999f3ff1a05d` | **Size**: 301 chars

**Classes**: InstrumentRentalSearchObject
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

---

## File: `eNote\eNote.Application\Features\Rentals\InstrumentRentals\RentalCreateRequest.cs`
**Hash**: `c5e467687902` | **Size**: 272 chars

**Classes**: RentalCreateRequest
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

---

## File: `eNote\eNote.Application\Features\Rentals\InstrumentRentals\RentalQueryableExtensions.cs`
**Hash**: `c5b49381b830` | **Size**: 503 chars

**Classes**: RentalQueryableExtensions
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

---

## File: `eNote\eNote.Application\Features\Rentals\InstrumentRentals\RentalStatusResponse.cs`
**Hash**: `64c8da3d0f2f` | **Size**: 141 chars

**Classes**: RentalStatusResponse
```cs
namespace eNote.Application.Features.Rentals.InstrumentRentals;

public class RentalStatusResponse
{
    public string? Note { get; set; }
}

```

---

## File: `eNote\eNote.Application\Features\Rentals\InstrumentRentals\Services\IRentalCommandService.cs`
**Hash**: `f3b14d3bd63e` | **Size**: 1061 chars

**Classes**: 
**Interfaces**: IRentalCommandService
```cs
namespace eNote.Application.Features.Rentals.InstrumentRentals.Services;

public interface IRentalCommandService
{
    Task<InstrumentRentalDto> CreateRequestAsync(RentalCreateRequest request, CancellationToken cancellationToken = default);
    Task<InstrumentRentalDto> ApproveAsync(int rentalId, RentalStatusResponse response, CancellationToken cancellationToken = default);
    Task<InstrumentRentalDto> RejectAsync(int rentalId, RentalStatusResponse response, CancellationToken cancellationToken = default);
    Task<InstrumentRentalDto> PickupAsync(int rentalId, RentalStatusResponse response, CancellationToken cancellationToken = default);
    Task<InstrumentRentalDto> CompleteAsync(int rentalId, RentalStatusResponse response, CancellationToken cancellationToken = default);
    Task<InstrumentRentalDto> CancelAsync(int rentalId, RentalStatusResponse response, CancellationToken cancellationToken = default);
    Task<InstrumentRentalDto> ReturnEarlyAsync(int rentalId, RentalStatusResponse response, CancellationToken cancellationToken = default);
}

```

---

## File: `eNote\eNote.Application\Features\Rentals\InstrumentRentals\Services\IRentalQueryService.cs`
**Hash**: `a05719e5dd87` | **Size**: 757 chars

**Classes**: 
**Interfaces**: IRentalQueryService
```cs
using eNote.Application.Common.Paging;
using eNote.Application.Features.Rentals.InstrumentRentals;

namespace eNote.Application.Features.Rentals.InstrumentRentals.Services;

public interface IRentalQueryService
{
    Task<InstrumentRentalDto> GetByIdForStudentAsync(int rentalId, CancellationToken cancellationToken = default);
    Task<InstrumentRentalDto> GetByIdForStoreAsync(int rentalId, CancellationToken cancellationToken = default);
    Task<PagedResult<InstrumentRentalDto>> GetPagedForStudentAsync(InstrumentRentalSearchObject searchObject, CancellationToken cancellationToken = default);
    Task<PagedResult<InstrumentRentalDto>> GetPagedForStoreAsync(InstrumentRentalSearchObject searchObject, CancellationToken cancellationToken = default);
}

```

---

## File: `eNote\eNote.Application\Features\Rentals\InstrumentRentals\Services\RentalCommandService.cs`
**Hash**: `a2cc07c731d4` | **Size**: 9045 chars

**Classes**: RentalCommandService
### Key Cross-Cutting Interactions
- Uses **ICurrentActor** → Current actor resolution
- Uses **IAppDbContext|DbContext** → Persistence boundary

```cs
using eNote.Domain.Entities;
using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Application.Features.Rentals.InstrumentRentals.Billing;
using eNote.Application.Features.Rentals.InstrumentRentals.StateMachine;
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

            _ = await context.Set<Instrument>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == request.InstrumentId && x.IsActive, cancellationToken) ?? throw new NotFoundException(Messages.InstrumentNotFound);

            var locked = await context.Set<InstrumentRental>()
                .AnyAsync(x => x.InstrumentId == request.InstrumentId && InstrumentRentalStatusSets.Blocking.Contains(x.RentalStatus), cancellationToken);

            if (locked)
            {
                throw new BusinessException(Messages.InstrumentReservedOrRented);
            }

            var alreadyPending = await context.Set<InstrumentRental>()
                .AnyAsync(x => x.InstrumentId == request.InstrumentId && x.StudentProfileId == studentProfileId && x.RentalStatus == InstrumentRentalStatus.Pending, cancellationToken);

            if (alreadyPending)
            {
                throw new BusinessException(Messages.RentalPendingRequired);
            }

            var rental = new InstrumentRental(request.InstrumentId, studentProfileId, clock.UtcNow, request.Note)
            {
                CreatedById = actor.UserId
            };

            context.Set<InstrumentRental>().Add(rental);
            await context.SaveChangesAsync(cancellationToken);

            var dto = await LoadDtoAsync(rental.Id, cancellationToken);
            await notificationDispatcher.DispatchCreatedAsync(dto, actor.UserId);

            return dto;
        }, cancellationToken);

        return dto;
    }

    public Task<InstrumentRentalDto> ApproveAsync(int rentalId, RentalStatusResponse response, CancellationToken cancellationToken = default) => ExecuteStoreTransitionAsync(rentalId, RentalTrigger.Approve, response, cancellationToken);

    public Task<InstrumentRentalDto> RejectAsync(int rentalId, RentalStatusResponse response, CancellationToken cancellationToken = default) => ExecuteStoreTransitionAsync(rentalId, RentalTrigger.Reject, response, cancellationToken);

    public Task<InstrumentRentalDto> PickupAsync(int rentalId, RentalStatusResponse response, CancellationToken cancellationToken = default) => ExecuteStoreTransitionAsync(rentalId, RentalTrigger.Pickup, response, cancellationToken);

    public Task<InstrumentRentalDto> CompleteAsync(int rentalId, RentalStatusResponse response, CancellationToken cancellationToken = default) => ExecuteStoreTransitionAsync(rentalId, RentalTrigger.Complete, response, cancellationToken);

    public Task<InstrumentRentalDto> ReturnEarlyAsync(int rentalId, RentalStatusResponse response, CancellationToken cancellationToken = default) => ExecuteStoreTransitionAsync(rentalId, RentalTrigger.ReturnEarly, response, cancellationToken);

    public async Task<InstrumentRentalDto> CancelAsync(int rentalId, RentalStatusResponse response, CancellationToken cancellationToken = default)
    {
        var dto = await ExecuteInTransactionAsync(async () =>
        {
            var rental = await LoadForStudentAsync(rentalId, actor.UserId, cancellationToken);
            var dto = await ExecuteTransitionAsync(rental, RentalTrigger.Cancel, RentalActor.Student, actor.UserId, response, cancellationToken);

            await notificationDispatcher.DispatchTransitionAsync(dto, RentalTrigger.Cancel, actor.UserId);

            return dto;
        }, cancellationToken);

        return dto;
    }

    private Task<InstrumentRentalDto> ExecuteStoreTransitionAsync(int rentalId, RentalTrigger trigger, RentalStatusResponse response, CancellationToken cancellationToken) => ExecuteInTransactionAsync(async () =>
    {
        var storeId = await actor.GetCurrentStoreIdAsync(cancellationToken);
        var rental = await LoadForStoreAsync(rentalId, storeId, cancellationToken);
        var dto = await ExecuteTransitionAsync(rental, trigger, RentalActor.StoreEmployee, actor.UserId, response, cancellationToken);

        await notificationDispatcher.DispatchTransitionAsync(dto, trigger, actor.UserId);

        return dto;
    }, cancellationToken);

    private async Task<InstrumentRentalDto> ExecuteTransitionAsync(InstrumentRental rental, RentalTrigger trigger, RentalActor actor, int userId, RentalStatusResponse? response, CancellationToken cancellationToken)
    {
        var hasConflict = false;

        if (trigger is RentalTrigger.Approve or RentalTrigger.Pickup)
        {
            hasConflict = await context.Set<InstrumentRental>()
                .AnyAsync(x => x.InstrumentId == rental.InstrumentId && x.Id != rental.Id && InstrumentRentalStatusSets.Blocking.Contains(x.RentalStatus), cancellationToken);
        }

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
            await SaveWithLockConflictMessageAsync(Messages.InstrumentReservedOrRented, cancellationToken);
        }
        else
        {
            await context.SaveChangesAsync(cancellationToken);
        }

        return await LoadDtoAsync(rental.Id, cancellationToken);
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
        catch (DbUpdateException)
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

```

---

## File: `eNote\eNote.Application\Features\Rentals\InstrumentRentals\Services\RentalQueryService.cs`
**Hash**: `709beabf724c` | **Size**: 3170 chars

**Classes**: RentalQueryService
### Key Cross-Cutting Interactions
- Uses **ICurrentActor** → Current actor resolution
- Uses **IAppDbContext|DbContext** → Persistence boundary

```cs
using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Paging;
using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Application.Features.Rentals.InstrumentRentals.Billing;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using eNote.Domain.Entities.Rentals;

namespace eNote.Application.Features.Rentals.InstrumentRentals.Services;

public sealed class RentalQueryService(IAppDbContext context, IMapper mapper, ICurrentActor actor, IClock clock) : IRentalQueryService
{
    public async Task<InstrumentRentalDto> GetByIdForStudentAsync(int rentalId, CancellationToken cancellationToken = default)
    {
        var entity = await FindRentalAsync(context.Set<InstrumentRental>().Where(x => x.Id == rentalId && x.StudentProfile.AppUserId == actor.UserId), cancellationToken);

        var dto = mapper.Map<InstrumentRentalDto>(entity);

        RentalBilling.ApplyBilling(entity, dto, clock.UtcNow);

        return dto;
    }

    public Task<PagedResult<InstrumentRentalDto>> GetPagedForStudentAsync(InstrumentRentalSearchObject search, CancellationToken cancellationToken = default) => GetPagedAsync(context.Set<InstrumentRental>()
        .Where(x => x.StudentProfile.AppUserId == actor.UserId), search, cancellationToken);

    public async Task<InstrumentRentalDto> GetByIdForStoreAsync(int rentalId, CancellationToken cancellationToken = default)
    {
        var storeId = await actor.GetCurrentStoreIdAsync(cancellationToken);

        var entity = await FindRentalAsync(context.Set<InstrumentRental>().Where(x => x.Id == rentalId && x.Instrument.MusicStoreId == storeId), cancellationToken);

        var dto = mapper.Map<InstrumentRentalDto>(entity);

        RentalBilling.ApplyBilling(entity, dto, clock.UtcNow);

        return dto;
    }

    public async Task<PagedResult<InstrumentRentalDto>> GetPagedForStoreAsync(InstrumentRentalSearchObject search, CancellationToken cancellationToken = default)
    {
        var storeId = await actor.GetCurrentStoreIdAsync(cancellationToken);

        return await GetPagedAsync(context.Set<InstrumentRental>().Where(x => x.Instrument.MusicStoreId == storeId), search, cancellationToken);
    }

    private async Task<PagedResult<InstrumentRentalDto>> GetPagedAsync(IQueryable<InstrumentRental> query, InstrumentRentalSearchObject search, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;

        return await query.AsNoTracking().WithRentalDetails().ApplySearch(search).OrderByDescending(x => x.RequestedAt).ToPagedResultAsync(search, entity =>
        {
            var dto = mapper.Map<InstrumentRentalDto>(entity);
            RentalBilling.ApplyBilling(entity, dto, now);

            return dto;
        }, ct: cancellationToken);
    }

    private static async Task<InstrumentRental> FindRentalAsync(IQueryable<InstrumentRental> query, CancellationToken cancellationToken) => await query
        .AsNoTracking().WithRentalDetails().FirstOrDefaultAsync(cancellationToken) ?? throw new NotFoundException(Messages.NotFound);
}

```

---

## File: `eNote\eNote.Application\Features\Rentals\InstrumentRentals\StateMachine\IRentalStateMachine.cs`
**Hash**: `ddf6fa93a4a3` | **Size**: 271 chars

**Classes**: 
**Interfaces**: IRentalStateMachine
```cs
using eNote.Domain.Entities.Rentals;

namespace eNote.Application.Features.Rentals.InstrumentRentals.StateMachine;

public interface IRentalStateMachine
{
    RentalTransitionResult Fire(InstrumentRental rental, RentalTrigger trigger, RentalTransitionContext context);
}

```

---

## File: `eNote\eNote.Application\Features\Rentals\InstrumentRentals\StateMachine\RentalActor.cs`
**Hash**: `9f00c147ec4a` | **Size**: 137 chars

```cs
namespace eNote.Application.Features.Rentals.InstrumentRentals.StateMachine;

public enum RentalActor
{
    StoreEmployee,
    Student
}

```

---

## File: `eNote\eNote.Application\Features\Rentals\InstrumentRentals\StateMachine\RentalStateMachine.cs`
**Hash**: `761ef80d3ecf` | **Size**: 7823 chars

**Classes**: RentalStateMachine
```cs
using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Time;
using eNote.Domain.Enums;
using eNote.Domain.Entities.Rentals;

namespace eNote.Application.Features.Rentals.InstrumentRentals.StateMachine;

public sealed class RentalStateMachine(IClock clock) : IRentalStateMachine
{
    private static readonly IReadOnlyList<TransitionDefinition> Transitions = CreateTransitions();

    public RentalTransitionResult Fire(InstrumentRental rental, RentalTrigger trigger, RentalTransitionContext context)
    {
        var transition = FindTransition(rental.RentalStatus, trigger, context.Actor) ?? throw new BusinessException(GetInvalidTransitionMessage(trigger, context.Actor));

        transition.Guard?.Invoke(rental, context);
        transition.Apply(rental, context, clock);

        return new RentalTransitionResult(transition.UsesInstrumentLock);
    }

    private static TransitionDefinition? FindTransition(InstrumentRentalStatus currentStatus, RentalTrigger trigger, RentalActor actor) => Transitions.FirstOrDefault(t => t.From == currentStatus && t.Trigger == trigger && t.Actors.Contains(actor));

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

    private static void GuardNoInstrumentLockConflict(RentalTransitionContext context)
    {
        if (context.HasInstrumentLockConflict)
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
            Guard: (rental, context) =>
            {
                GuardInstrumentActive(rental);
                GuardNoInstrumentLockConflict(context);
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
            Guard: null,
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
            Guard: (rental, _) => GuardNotPickedUp(rental),
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
            Guard: (rental, context) =>
            {
                GuardInstrumentActive(rental);
                if (rental.PickedUpAt.HasValue) { throw new BusinessException(Messages.RentalAlreadyPickedUp); }
                GuardNoInstrumentLockConflict(context);
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
            Guard: (rental, _) => GuardNotPickedUp(rental),
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
            Guard: (rental, _) => GuardNotReturned(rental),
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
            Guard: (rental, _) =>
            {
                GuardNotReturned(rental);
                GuardPickedUp(rental);
            },
            Apply: (rental, context, time) =>
            {
                var note = !string.IsNullOrWhiteSpace(context.Response?.Note) ? context.Response.Note : rental.Note;
                rental.ReturnEarly(time.UtcNow, note);
                ApplyAuditFields(rental, context, time);
            },
            UsesInstrumentLock: false),
    ];

    private sealed record TransitionDefinition(InstrumentRentalStatus From, RentalTrigger Trigger, RentalActor[] Actors, Action<InstrumentRental, RentalTransitionContext>? Guard, Action<InstrumentRental, RentalTransitionContext, IClock> Apply, bool UsesInstrumentLock);
}

```

---

## File: `eNote\eNote.Application\Features\Rentals\InstrumentRentals\StateMachine\RentalTransitionContext.cs`
**Hash**: `ce702bce69f7` | **Size**: 409 chars

**Classes**: RentalTransitionContext
```cs
using eNote.Application.Features.Rentals.InstrumentRentals;

namespace eNote.Application.Features.Rentals.InstrumentRentals.StateMachine;

public sealed class RentalTransitionContext
{
    public required int UserId { get; init; }
    public required RentalActor Actor { get; init; }
    public required bool HasInstrumentLockConflict { get; init; }
    public RentalStatusResponse? Response { get; init; }
}

```

---

## File: `eNote\eNote.Application\Features\Rentals\InstrumentRentals\StateMachine\RentalTransitionResult.cs`
**Hash**: `7bd749a3f2f5` | **Size**: 148 chars

```cs
namespace eNote.Application.Features.Rentals.InstrumentRentals.StateMachine;

public sealed record RentalTransitionResult(bool UsesInstrumentLock);

```

---

## File: `eNote\eNote.Application\Features\Rentals\InstrumentRentals\StateMachine\RentalTrigger.cs`
**Hash**: `a21166fbb38a` | **Size**: 187 chars

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

---

## File: `eNote\eNote.Domain\Enums\InstrumentRentalStatus.cs`
**Hash**: `ed7d5dcb9f7d` | **Size**: 197 chars

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

---

## File: `eNote\eNote.Domain\Enums\InstrumentRentalStatusExtensions.cs`
**Hash**: `db395448d8b8` | **Size**: 480 chars

**Classes**: InstrumentRentalStatusExtensions
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

---

## File: `eNote\eNote.Domain\Enums\InstrumentRentalStatusSets.cs`
**Hash**: `44bb97d2a6fc` | **Size**: 617 chars

**Classes**: InstrumentRentalStatusSets
```cs
using System.Collections.Frozen;

namespace eNote.Domain.Enums;

public static class InstrumentRentalStatusSets
{
    public static readonly FrozenSet<InstrumentRentalStatus> Blocking = new InstrumentRentalStatus[]
    {
        InstrumentRentalStatus.Approved,
        InstrumentRentalStatus.Active
    }.ToFrozenSet();

    public static readonly FrozenSet<InstrumentRentalStatus> History = new InstrumentRentalStatus[]
    {
        InstrumentRentalStatus.Approved,
        InstrumentRentalStatus.Active,
        InstrumentRentalStatus.Completed,
        InstrumentRentalStatus.ReturnedEarly
    }.ToFrozenSet();
}
```

---

