using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Time;
using eNote.Domain.Entities;
using eNote.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.InstrumentRentals.StateMachine
{
    public sealed class RentalStateMachine(IClock clock) : IRentalStateMachine
    {
        private static readonly IReadOnlyList<TransitionDefinition> Transitions = CreateTransitions();

        public async Task<RentalTransitionResult> FireAsync(InstrumentRental rental, RentalTrigger trigger, RentalTransitionContext context, CancellationToken cancellationToken = default)
        {
            TransitionDefinition transition = FindTransition(rental.RentalStatus, trigger, context.Actor) ?? throw new BusinessException(GetInvalidTransitionMessage(trigger, context.Actor));

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
            bool conflict = await context.Db.Set<InstrumentRental>()
                .AnyAsync(x =>
                    x.InstrumentId == rental.InstrumentId &&
                    x.Id != rental.Id &&
                    (x.RentalStatus == InstrumentRentalStatus.Approved ||
                     x.RentalStatus == InstrumentRentalStatus.Active),
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
                To: InstrumentRentalStatus.Approved,
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
                To: InstrumentRentalStatus.Rejected,
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
                To: InstrumentRentalStatus.Canceled,
                Actors: [RentalActor.Student],
                GuardAsync: (rental, _, _) =>
                {
                    GuardNotPickedUp(rental);
                    return Task.CompletedTask;
                },
                Apply: (rental, context, time) =>
                {
                    string? note = !string.IsNullOrWhiteSpace(context.Response?.Note) ? context.Response.Note : rental.Note;
                    rental.Cancel(time.UtcNow, note);
                    ApplyAuditFields(rental, context, time);
                },
                UsesInstrumentLock: false),

            new(
                From: InstrumentRentalStatus.Approved,
                Trigger: RentalTrigger.Pickup,
                To: InstrumentRentalStatus.Active,
                Actors: [RentalActor.StoreEmployee],
                GuardAsync: async (rental, context, ct) =>
                {
                    GuardInstrumentActive(rental);

                    if (rental.PickedUpAt.HasValue) { throw new BusinessException(Messages.RentalAlreadyPickedUp); } await GuardNoInstrumentLockConflictAsync(rental, context, ct);
                },
                Apply: (rental, context, time) =>
                {
                    string? note = !string.IsNullOrWhiteSpace(context.Response?.Note) ? context.Response.Note : rental.Note;
                    rental.Pickup(time.UtcNow, note);
                    ApplyAuditFields(rental, context, time);
                },
                UsesInstrumentLock: true),

            new(
                From: InstrumentRentalStatus.Approved,
                Trigger: RentalTrigger.Cancel,
                To: InstrumentRentalStatus.Canceled,
                Actors: [RentalActor.Student],
                GuardAsync: (rental, _, _) =>
                {
                    GuardNotPickedUp(rental);
                    return Task.CompletedTask;
                },
                Apply: (rental, context, time) =>
                {
                    string? note = !string.IsNullOrWhiteSpace(context.Response?.Note) ? context.Response.Note : rental.Note;
                    rental.Cancel(time.UtcNow, note);
                    ApplyAuditFields(rental, context, time);
                },
                UsesInstrumentLock: false),

            new(
                From: InstrumentRentalStatus.Active,
                Trigger: RentalTrigger.Complete,
                To: InstrumentRentalStatus.Completed,
                Actors: [RentalActor.StoreEmployee],
                GuardAsync: (rental, _, _) =>
                {
                    GuardNotReturned(rental);
                    return Task.CompletedTask;
                },
                Apply: (rental, context, time) =>
                {
                    string? note = !string.IsNullOrWhiteSpace(context.Response?.Note) ? context.Response.Note : rental.Note;
                    rental.Complete(time.UtcNow, note);
                    ApplyAuditFields(rental, context, time);
                },
                UsesInstrumentLock: false),

            new(
                From: InstrumentRentalStatus.Active,
                Trigger: RentalTrigger.ReturnEarly,
                To: InstrumentRentalStatus.ReturnedEarly,
                Actors: [RentalActor.StoreEmployee],
                GuardAsync: (rental, _, _) =>
                {
                    GuardNotReturned(rental);
                    GuardPickedUp(rental);
                    return Task.CompletedTask;
                },
                Apply: (rental, context, time) =>
                {
                    string? note = !string.IsNullOrWhiteSpace(context.Response?.Note) ? context.Response.Note : rental.Note;
                    rental.ReturnEarly(time.UtcNow, note);
                    ApplyAuditFields(rental, context, time);
                },
                UsesInstrumentLock: false),
        ];

        private sealed record TransitionDefinition(
            InstrumentRentalStatus From, RentalTrigger Trigger, InstrumentRentalStatus To, RentalActor[] Actors,
            Func<InstrumentRental, RentalTransitionContext, CancellationToken, Task>? GuardAsync, Action<InstrumentRental,
            RentalTransitionContext, IClock> Apply, bool UsesInstrumentLock
        );
    }
}
