namespace eNote.Application.Features.Rentals.Payments;

public sealed record RefundRequest(long? AmountCents);

public sealed record CreatePaymentIntentResponse(
    int RentalId,
    string PaymentIntentId,
    string ClientSecret,
    long AmountCents,
    string Currency,
    PaymentStatus Status);

public sealed record RentalPaymentDto(
    int Id,
    int RentalId,
    string PaymentIntentId,
    long AmountCents,
    string Currency,
    PaymentStatus Status,
    DateTime? PaidAt,
    DateTime? RefundedAt,
    long? RefundedCents);
