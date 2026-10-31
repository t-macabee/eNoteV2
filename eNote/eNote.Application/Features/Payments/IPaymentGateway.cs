namespace eNote.Application.Features.Payments;

public sealed record PaymentIntentData(
    string Id,
    string ClientSecret,
    PaymentIntentStatus Status,
    long AmountCents,
    string Currency);

public sealed record RefundData(
    string Id,
    long AmountCents,
    RefundStatus Status);

public interface IPaymentGateway
{
    Task<PaymentIntentData> CreatePaymentIntentAsync(
        long amountCents,
        string currency,
        IReadOnlyDictionary<string, string> metadata,
        string idempotencyKey,
        string statementDescriptorSuffix,
        CancellationToken cancellationToken = default);

    Task<PaymentIntentData> RetrievePaymentIntentAsync(
        string paymentIntentId,
        CancellationToken cancellationToken = default);

    Task<PaymentIntentData> CancelPaymentIntentAsync(
        string paymentIntentId,
        CancellationToken cancellationToken = default);

    Task<RefundData> CreateRefundAsync(
        string paymentIntentId,
        long? amountCents,
        string reason,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RefundData>> ListRefundsAsync(
        string paymentIntentId,
        CancellationToken cancellationToken = default);
}
