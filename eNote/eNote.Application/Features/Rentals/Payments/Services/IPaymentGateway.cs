namespace eNote.Application.Features.Rentals.Payments.Services;

public sealed record PaymentIntentData(
    string Id,
    string ClientSecret,
    string Status,
    long AmountCents,
    string Currency);

public sealed record RefundData(
    string Id,
    long AmountCents,
    string Status);

/// <summary>
/// Port for the Stripe payment provider. Kept in the Application layer so payment
/// orchestration can be unit-tested against a fake gateway; the concrete
/// <c>StripePaymentGateway</c> lives in Infrastructure.
/// </summary>
public interface IPaymentGateway
{
    Task<PaymentIntentData> CreatePaymentIntentAsync(
        long amountCents,
        string currency,
        IReadOnlyDictionary<string, string> metadata,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<PaymentIntentData> RetrievePaymentIntentAsync(
        string paymentIntentId,
        CancellationToken cancellationToken = default);

    Task<RefundData> CreateRefundAsync(
        string paymentIntentId,
        long? amountCents,
        string reason,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
}
