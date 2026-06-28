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

    private sealed record TransitionDefinition(
        InstrumentRentalStatus From, RentalTrigger Trigger, RentalActor[] Actors,
        Action<InstrumentRental, RentalTransitionContext>? Guard,
        Action<InstrumentRental, RentalTransitionContext, IClock> Apply,
        bool UsesInstrumentLock
    );
}
