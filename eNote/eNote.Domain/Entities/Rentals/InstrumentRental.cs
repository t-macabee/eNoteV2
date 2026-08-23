using eNote.Domain.Enums;
using eNote.Domain.Shared;

namespace eNote.Domain.Entities.Rentals;

public sealed class InstrumentRental : AuditableEntity, ITenantScoped
{
    public int StudentProfileId { get; private set; }
    public Student StudentProfile { get; private set; } = null!;
    public int InstrumentId { get; private set; }
    public Instrument Instrument { get; private set; } = null!;
    public int MusicStoreId { get; private set; }

    public InstrumentRentalStatus RentalStatus { get; private set; }
    public string? RequestNote { get; private set; }
    public string? Note { get; private set; }

    public DateTime RequestedAt { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public DateTime? RejectedAt { get; private set; }
    public DateTime? PickedUpAt { get; private set; }
    public DateTime? ReturnedAt { get; private set; }

    public int? ApprovedById { get; private set; }
    public int? RejectedById { get; private set; }

    public decimal Fee { get; private set; }

    private const int DaysPerBillingCycle = 30;

    /// <summary>
    /// Computes the charges due for this rental as of <paramref name="now"/>.
    /// Nothing is billed before pickup or when the status is not billing-eligible.
    /// An early return is prorated by day (fee / 30, capped at the monthly fee);
    /// every other billable status charges whole months (ceiling of days / 30, minimum 1).
    /// </summary>
    public RentalCharges CalculateCharges(DateTime now)
    {
        if (!PickedUpAt.HasValue)
        {
            return new RentalCharges(null, null, null, null, false);
        }

        if (!RentalStatus.IsBillingEligible())
        {
            return new RentalCharges(null, null, null, null, false);
        }

        var start = PickedUpAt.Value;
        var end = ReturnedAt ?? now;

        if (end < start)
        {
            end = start;
        }

        var daysCharged = (int)Math.Ceiling((end - start).TotalDays);

        if (daysCharged < 1)
        {
            daysCharged = 1;
        }

        if (RentalStatus == InstrumentRentalStatus.ReturnedEarly)
        {
            var dailyFee = Fee / DaysPerBillingCycle;
            var prorated = daysCharged * dailyFee;
            var totalFee = prorated > Fee ? Fee : prorated;

            return new RentalCharges(MonthsCharged: null, DaysCharged: daysCharged, DailyFee: decimal.Round(dailyFee, 2), TotalFee: decimal.Round(totalFee, 2), IsProrated: true);
        }

        var monthsCharged = (int)Math.Ceiling((end - start).TotalDays / DaysPerBillingCycle);

        if (monthsCharged < 1)
        {
            monthsCharged = 1;
        }

        return new RentalCharges(MonthsCharged: monthsCharged, DaysCharged: null, DailyFee: null, TotalFee: monthsCharged * Fee, IsProrated: false);
    }

    private InstrumentRental()
    {
    }

    public InstrumentRental(int instrumentId, int studentProfileId, int musicStoreId, DateTime requestedAt, string? note)
    {
        InstrumentId = instrumentId;
        StudentProfileId = studentProfileId;
        MusicStoreId = musicStoreId;
        RequestedAt = requestedAt;
        RequestNote = note;
        RentalStatus = InstrumentRentalStatus.Pending;
    }

    public static InstrumentRental CreateWithInstrument(int instrumentId, int studentProfileId, int musicStoreId, DateTime requestedAt, string? note, Instrument instrument)
    {
        var rental = new InstrumentRental(instrumentId, studentProfileId, musicStoreId, requestedAt, note)
        {
            Instrument = instrument
        };

        return rental;
    }

    public void Approve(decimal fee, string? note, DateTime approvedAt, int approvedById)
    {
        Fee = fee;
        Note = note;
        ApprovedAt = approvedAt;
        ApprovedById = approvedById;
        RentalStatus = InstrumentRentalStatus.Approved;
    }

    public void Reject(DateTime rejectedAt, string? note, int rejectedById)
    {
        Note = note;
        RejectedAt = rejectedAt;
        RejectedById = rejectedById;
        RentalStatus = InstrumentRentalStatus.Rejected;
    }

    public void Cancel(DateTime returnedAt, string? note)
    {
        Note = note;
        ReturnedAt = returnedAt;
        RentalStatus = InstrumentRentalStatus.Canceled;
    }

    public void Pickup(DateTime pickedUpAt, string? note = null)
    {
        PickedUpAt = pickedUpAt;
        RentalStatus = InstrumentRentalStatus.Active;
        if (note != null)
        {
            Note = note;
        }
    }

    public void Complete(DateTime returnedAt, string? note)
    {
        Note = note;
        ReturnedAt = returnedAt;
        RentalStatus = InstrumentRentalStatus.Completed;
    }

    public void ReturnEarly(DateTime returnedAt, string? note)
    {
        Note = note;
        ReturnedAt = returnedAt;
        RentalStatus = InstrumentRentalStatus.ReturnedEarly;
    }

    /// <summary>
    /// Validates and applies a status transition against the current <see cref="RentalStatus"/>,
    /// dispatching to the matching mutator (<see cref="Approve"/>, <see cref="Reject"/>, etc.).
    /// Folds what used to be the separate RentalStateMachine's guard/dispatch table onto the
    /// entity itself, since the guards only ever inspect and mutate this entity's own state.
    /// </summary>
    public Result<RentalTransitionResult> Transition(RentalTrigger trigger, RentalTransitionContext context, DateTime now)
    {
        var transition = FindTransition(RentalStatus, trigger, context.Actor);
        if (transition is null)
        {
            return Result<RentalTransitionResult>.Failure(GetInvalidTransitionMessage(trigger, context.Actor));
        }

        var guardError = transition.Guard?.Invoke(this, context);
        if (guardError is not null)
        {
            return Result<RentalTransitionResult>.Failure(guardError);
        }

        transition.Apply(this, context, now);
        return Result<RentalTransitionResult>.Success(new RentalTransitionResult(transition.UsesInstrumentLock));
    }

    private static TransitionDefinition? FindTransition(InstrumentRentalStatus currentStatus, RentalTrigger trigger, RentalActor actor) =>
        Transitions.FirstOrDefault(t => t.From == currentStatus && t.Trigger == trigger && t.Actors.Contains(actor));

    private static string GetInvalidTransitionMessage(RentalTrigger trigger, RentalActor actor)
    {
        if (Transitions.Any(t => t.Trigger == trigger && t.Actors.Contains(actor)))
        {
            return GetWrongStateMessage(trigger);
        }

        return RentalAccessDeniedMessage;
    }

    private static string GetWrongStateMessage(RentalTrigger trigger) => trigger switch
    {
        RentalTrigger.Approve => RentalApprovePendingOnlyMessage,
        RentalTrigger.Reject => RentalRejectPendingOnlyMessage,
        RentalTrigger.Pickup => RentalPickupApprovedOnlyMessage,
        RentalTrigger.Complete => RentalCompleteActiveOnlyMessage,
        RentalTrigger.Cancel => RentalCancelPendingOrApprovedOnlyMessage,
        RentalTrigger.ReturnEarly => RentalEarlyReturnActiveOnlyMessage,
        _ => BadRequestMessage
    };

    private static string? GuardNoInstrumentLockConflict(RentalTransitionContext context) =>
        context.HasInstrumentLockConflict ? InstrumentReservedOrRentedMessage : null;

    private static string? GuardInstrumentActive(RentalTransitionContext context) =>
        !context.IsInstrumentActive ? InstrumentInactiveMessage : null;

    private static string? GuardNotPickedUp(InstrumentRental rental) =>
        rental.PickedUpAt.HasValue ? RentalCancelBlockedAfterPickupMessage : null;

    private static string? GuardNotReturned(InstrumentRental rental) =>
        rental.ReturnedAt.HasValue ? RentalAlreadyCompletedMessage : null;

    private static string? GuardPickedUp(InstrumentRental rental) =>
        !rental.PickedUpAt.HasValue ? RentalNotPickedUpMessage : null;

    private static void ApplyAuditFields(InstrumentRental rental, RentalTransitionContext context) =>
        rental.UpdatedById = context.UserId;

    private static IReadOnlyList<TransitionDefinition> CreateTransitions() =>
    [
        new(
            From: InstrumentRentalStatus.Pending,
            Trigger: RentalTrigger.Approve,
            Actors: [RentalActor.StoreEmployee],
            Guard: (_, context) => GuardInstrumentActive(context) ?? GuardNoInstrumentLockConflict(context),
            Apply: (rental, context, now) =>
            {
                rental.Approve(context.MonthlyFee, context.ResponseNote, now, context.UserId);
                ApplyAuditFields(rental, context);
            },
            UsesInstrumentLock: true),

        new(
            From: InstrumentRentalStatus.Pending,
            Trigger: RentalTrigger.Reject,
            Actors: [RentalActor.StoreEmployee],
            Guard: null,
            Apply: (rental, context, now) =>
            {
                rental.Reject(now, context.ResponseNote, context.UserId);
                ApplyAuditFields(rental, context);
            },
            UsesInstrumentLock: false),

        new(
            From: InstrumentRentalStatus.Pending,
            Trigger: RentalTrigger.Cancel,
            Actors: [RentalActor.Student],
            Guard: (rental, _) => GuardNotPickedUp(rental),
            Apply: (rental, context, now) =>
            {
                var note = !string.IsNullOrWhiteSpace(context.ResponseNote) ? context.ResponseNote : rental.Note;
                rental.Cancel(now, note);
                ApplyAuditFields(rental, context);
            },
            UsesInstrumentLock: false),

        new(
            From: InstrumentRentalStatus.Approved,
            Trigger: RentalTrigger.Pickup,
            Actors: [RentalActor.StoreEmployee],
            Guard: (rental, context) => GuardInstrumentActive(context)
                ?? (rental.PickedUpAt.HasValue ? RentalAlreadyPickedUpMessage : null)
                ?? GuardNoInstrumentLockConflict(context),
            Apply: (rental, context, now) =>
            {
                var note = !string.IsNullOrWhiteSpace(context.ResponseNote) ? context.ResponseNote : rental.Note;
                rental.Pickup(now, note);
                ApplyAuditFields(rental, context);
            },
            UsesInstrumentLock: true),

        new(
            From: InstrumentRentalStatus.Approved,
            Trigger: RentalTrigger.Cancel,
            Actors: [RentalActor.Student],
            Guard: (rental, _) => GuardNotPickedUp(rental),
            Apply: (rental, context, now) =>
            {
                var note = !string.IsNullOrWhiteSpace(context.ResponseNote) ? context.ResponseNote : rental.Note;
                rental.Cancel(now, note);
                ApplyAuditFields(rental, context);
            },
            UsesInstrumentLock: false),

        new(
            From: InstrumentRentalStatus.Active,
            Trigger: RentalTrigger.Complete,
            Actors: [RentalActor.StoreEmployee],
            Guard: (rental, _) => GuardNotReturned(rental),
            Apply: (rental, context, now) =>
            {
                var note = !string.IsNullOrWhiteSpace(context.ResponseNote) ? context.ResponseNote : rental.Note;
                rental.Complete(now, note);
                ApplyAuditFields(rental, context);
            },
            UsesInstrumentLock: false),

        new(
            From: InstrumentRentalStatus.Active,
            Trigger: RentalTrigger.ReturnEarly,
            Actors: [RentalActor.StoreEmployee],
            Guard: (rental, _) => GuardNotReturned(rental) ?? GuardPickedUp(rental),
            Apply: (rental, context, now) =>
            {
                var note = !string.IsNullOrWhiteSpace(context.ResponseNote) ? context.ResponseNote : rental.Note;
                rental.ReturnEarly(now, note);
                ApplyAuditFields(rental, context);
            },
            UsesInstrumentLock: false),
    ];

    private static readonly IReadOnlyList<TransitionDefinition> Transitions = CreateTransitions();

    // Domain-owned transition failure messages. eNote.Application.Common.Localization.Messages
    // (the app-wide message catalog) is an Application-layer type that Domain cannot reference
    // without inverting the project dependency (eNote.Domain has zero project references), so
    // these mirror that catalog's wording for the same conditions rather than sharing it.
    private const string RentalAccessDeniedMessage = "Nemate pravo nad ovim zahtjevom.";
    private const string RentalApprovePendingOnlyMessage = "Samo zahtjev na čekanju može biti odobren.";
    private const string RentalRejectPendingOnlyMessage = "Samo zahtjev na čekanju se može odbiti.";
    private const string RentalPickupApprovedOnlyMessage = "Samo odobreno iznajmljivanje se može preuzeti.";
    private const string RentalCompleteActiveOnlyMessage = "Samo aktivno iznajmljivanje se može završiti.";
    private const string RentalCancelPendingOrApprovedOnlyMessage = "Samo zahtjev na čekanju ili odobren zahtjev se može otkazati.";
    private const string RentalEarlyReturnActiveOnlyMessage = "Samo aktivno iznajmljivanje se može prijevremeno završiti.";
    private const string BadRequestMessage = "Neispravan zahtjev.";
    private const string InstrumentReservedOrRentedMessage = "Instrument je rezervisan ili već iznajmljen.";
    private const string InstrumentInactiveMessage = "Instrument nije aktivan.";
    private const string RentalCancelBlockedAfterPickupMessage = "Instrument je već preuzet, otkazivanje nije moguće.";
    private const string RentalAlreadyCompletedMessage = "Iznajmljivanje je već završeno.";
    private const string RentalNotPickedUpMessage = "Instrument nije preuzet.";
    private const string RentalAlreadyPickedUpMessage = "Instrument je već preuzet.";

    private sealed record TransitionDefinition(
        InstrumentRentalStatus From,
        RentalTrigger Trigger,
        RentalActor[] Actors,
        Func<InstrumentRental, RentalTransitionContext, string?>? Guard,
        Action<InstrumentRental, RentalTransitionContext, DateTime> Apply,
        bool UsesInstrumentLock);
}
