namespace eNote.Application.Features.Rentals.Payments;

/// <summary>
/// Request body for a store-initiated refund. The amount is optional; when omitted
/// the full charged amount is refunded. The server never accepts a client-supplied
/// charge amount for creating a payment intent.
/// </summary>
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
