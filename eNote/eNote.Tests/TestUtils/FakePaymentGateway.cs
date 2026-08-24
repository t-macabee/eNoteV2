using eNote.Application.Features.Rentals.Payments.Services;

namespace eNote.Tests.TestUtils;

/// <summary>
/// In-memory IPaymentGateway double for unit tests. Never touches Stripe:
/// returns stub pi_test_* / re_test_* ids and a fake client secret. Keeps a
/// record of every call so tests can assert what was sent to the gateway.
/// </summary>
public sealed class FakePaymentGateway : IPaymentGateway
{
    private readonly Dictionary<string, PaymentIntentData> _intents = new();

    public List<FakeGatewayCreateCall> CreateCalls { get; } = [];
    public List<FakeGatewayRetrieveCall> RetrieveCalls { get; } = [];
    public List<FakeGatewayRefundCall> RefundCalls { get; } = [];

    private int _nextIntentNumber = 1;
    private int _nextRefundNumber = 1;

    public Task<PaymentIntentData> CreatePaymentIntentAsync(
        long amountCents,
        string currency,
        IReadOnlyDictionary<string, string> metadata,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var id = $"pi_test_{_nextIntentNumber++}";
        var intent = new PaymentIntentData(id, $"{id}_secret", "requires_payment_method", amountCents, currency);
        _intents[id] = intent;
        CreateCalls.Add(new FakeGatewayCreateCall(id, amountCents, currency, metadata, idempotencyKey));
        return Task.FromResult(intent);
    }

    public Task<PaymentIntentData> RetrievePaymentIntentAsync(
        string paymentIntentId,
        CancellationToken cancellationToken = default)
    {
        if (!_intents.TryGetValue(paymentIntentId, out var intent))
        {
            throw new InvalidOperationException($"Unknown PaymentIntent {paymentIntentId}");
        }

        RetrieveCalls.Add(new FakeGatewayRetrieveCall(paymentIntentId));
        return Task.FromResult(intent);
    }

    public Task<RefundData> CreateRefundAsync(
        string paymentIntentId,
        long? amountCents,
        string reason,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var id = $"re_test_{_nextRefundNumber++}";
        RefundCalls.Add(new FakeGatewayRefundCall(id, paymentIntentId, amountCents, reason, idempotencyKey));
        return Task.FromResult(new RefundData(id, amountCents ?? 0, "succeeded"));
    }
}

public sealed record FakeGatewayCreateCall(
    string PaymentIntentId,
    long AmountCents,
    string Currency,
    IReadOnlyDictionary<string, string> Metadata,
    string IdempotencyKey);

public sealed record FakeGatewayRetrieveCall(string PaymentIntentId);

public sealed record FakeGatewayRefundCall(
    string RefundId,
    string PaymentIntentId,
    long? AmountCents,
    string Reason,
    string IdempotencyKey);
