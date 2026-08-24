namespace eNote.Contracts.Rentals;

public record RentalRefunded(
    int RentalId,
    int StudentUserId,
    int? ActorUserId,
    long RefundedCents,
    string InstrumentModel,
    string Title,
    string Body,
    DateTime OccurredAtUtc);
