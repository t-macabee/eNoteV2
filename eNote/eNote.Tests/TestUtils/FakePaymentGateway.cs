using eNote.Application.Features.Payments;

namespace eNote.Tests.TestUtils;

public sealed class FakePaymentGateway : IPaymentGateway
{
    private readonly Dictionary<string, PaymentIntentData> _intents = new();
    private readonly Dictionary<string, PaymentIntentData> _intentsByIdempotencyKey = new();
    private readonly Dictionary<string, List<RefundData>> _refundsByPaymentIntent = new();

    public List<FakeGatewayCreateCall> CreateCalls { get; } = [];
    public List<FakeGatewayRetrieveCall> RetrieveCalls { get; } = [];
    public List<FakeGatewayCancelCall> CancelCalls { get; } = [];
    public List<FakeGatewayRefundCall> RefundCalls { get; } = [];
    public List<FakeGatewayListRefundsCall> ListRefundCalls { get; } = [];

    public RefundStatus RefundStatus { get; set; } = RefundStatus.Succeeded;
    public Exception? RefundException { get; set; }
    public Func<string, Exception?>? CancelException { get; set; }

    private int _nextIntentNumber = 1;
    private int _nextRefundNumber = 1;

    public PaymentIntentData AddIntent(string paymentIntentId, long amountCents, string currency, PaymentIntentStatus status = PaymentIntentStatus.RequiresAction)
    {
        var intent = new PaymentIntentData(paymentIntentId, $"{paymentIntentId}_secret", status, amountCents, currency);
        _intents[paymentIntentId] = intent;
        return intent;
    }

    public void SetIntentStatus(string paymentIntentId, PaymentIntentStatus status)
    {
        if (!_intents.TryGetValue(paymentIntentId, out var intent))
        {
            throw new InvalidOperationException($"Unknown PaymentIntent {paymentIntentId}");
        }

        var updated = intent with { Status = status };
        _intents[paymentIntentId] = updated;

        foreach (var key in _intentsByIdempotencyKey.Where(entry => entry.Value.Id == paymentIntentId).Select(entry => entry.Key).ToList())
        {
            _intentsByIdempotencyKey[key] = updated;
        }
    }

    public void AddRefund(string paymentIntentId, string refundId, long amountCents, RefundStatus status = RefundStatus.Succeeded)
    {
        if (!_refundsByPaymentIntent.TryGetValue(paymentIntentId, out var refunds))
        {
            refunds = [];
            _refundsByPaymentIntent[paymentIntentId] = refunds;
        }

        refunds.Add(new RefundData(refundId, amountCents, status));
    }

    public Task<PaymentIntentData> CreatePaymentIntentAsync(
        long amountCents,
        string currency,
        IReadOnlyDictionary<string, string> metadata,
        string idempotencyKey,
        string statementDescriptorSuffix,
        CancellationToken cancellationToken = default)
    {
        if (!_intentsByIdempotencyKey.TryGetValue(idempotencyKey, out var intent))
        {
            var id = $"pi_test_{_nextIntentNumber++}";
            intent = new PaymentIntentData(id, $"{id}_secret", PaymentIntentStatus.RequiresAction, amountCents, currency);
            _intentsByIdempotencyKey[idempotencyKey] = intent;
            _intents[id] = intent;
        }

        CreateCalls.Add(new FakeGatewayCreateCall(intent.Id, amountCents, currency, metadata, idempotencyKey, statementDescriptorSuffix));
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

    public Task<PaymentIntentData> CancelPaymentIntentAsync(
        string paymentIntentId,
        CancellationToken cancellationToken = default)
    {
        if (!_intents.ContainsKey(paymentIntentId))
        {
            throw new InvalidOperationException($"Unknown PaymentIntent {paymentIntentId}");
        }

        CancelCalls.Add(new FakeGatewayCancelCall(paymentIntentId));

        if (CancelException?.Invoke(paymentIntentId) is { } exception)
        {
            throw exception;
        }

        SetIntentStatus(paymentIntentId, PaymentIntentStatus.Canceled);
        return Task.FromResult(_intents[paymentIntentId]);
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
        if (RefundException is not null)
        {
            throw RefundException;
        }

        return Task.FromResult(new RefundData(id, amountCents ?? 0, RefundStatus));
    }

    public Task<IReadOnlyList<RefundData>> ListRefundsAsync(
        string paymentIntentId,
        CancellationToken cancellationToken = default)
    {
        ListRefundCalls.Add(new FakeGatewayListRefundsCall(paymentIntentId));

        IReadOnlyList<RefundData> refunds = _refundsByPaymentIntent.TryGetValue(paymentIntentId, out var stored)
            ? stored.ToList()
            : [];

        return Task.FromResult(refunds);
    }
}

public sealed record FakeGatewayCreateCall(
    string PaymentIntentId,
    long AmountCents,
    string Currency,
    IReadOnlyDictionary<string, string> Metadata,
    string IdempotencyKey,
    string StatementDescriptorSuffix = "");

public sealed record FakeGatewayRetrieveCall(string PaymentIntentId);

public sealed record FakeGatewayCancelCall(string PaymentIntentId);

public sealed record FakeGatewayRefundCall(
    string RefundId,
    string PaymentIntentId,
    long? AmountCents,
    string Reason,
    string IdempotencyKey);

public sealed record FakeGatewayListRefundsCall(string PaymentIntentId);

public sealed class ThrowingPaymentGateway : IPaymentGateway
{
    public Exception ExceptionToThrow { get; init; } = new InvalidOperationException("Stripe is unavailable.");

    public Task<PaymentIntentData> CreatePaymentIntentAsync(
        long amountCents,
        string currency,
        IReadOnlyDictionary<string, string> metadata,
        string idempotencyKey,
        string statementDescriptorSuffix,
        CancellationToken cancellationToken = default)
        => throw ExceptionToThrow;

    public Task<PaymentIntentData> RetrievePaymentIntentAsync(
        string paymentIntentId,
        CancellationToken cancellationToken = default)
        => throw ExceptionToThrow;

    public Task<PaymentIntentData> CancelPaymentIntentAsync(
        string paymentIntentId,
        CancellationToken cancellationToken = default)
        => throw ExceptionToThrow;

    public Task<RefundData> CreateRefundAsync(
        string paymentIntentId,
        long? amountCents,
        string reason,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
        => throw ExceptionToThrow;

    public Task<IReadOnlyList<RefundData>> ListRefundsAsync(
        string paymentIntentId,
        CancellationToken cancellationToken = default)
        => throw ExceptionToThrow;
}
