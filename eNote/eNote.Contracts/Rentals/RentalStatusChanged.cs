namespace eNote.Contracts.Rentals;

public record RentalStatusChanged(int RentalId, int StudentUserId, int RecipientUserId, int? ActorUserId, string Status, string InstrumentModel, string Title, string Body, DateTime OccurredAtUtc);
