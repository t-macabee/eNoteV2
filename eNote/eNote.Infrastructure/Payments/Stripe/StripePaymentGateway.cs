using eNote.Application.Features.Payments;
using Microsoft.Extensions.Logging;
using Stripe;

namespace eNote.Infrastructure.Payments.Stripe;

public sealed class StripePaymentGateway : IPaymentGateway
{
    private readonly ILogger<StripePaymentGateway> _logger;
    private readonly Lazy<StripeClient> _client;

    public StripePaymentGateway(StripeOptions options, ILogger<StripePaymentGateway> logger)
    {
        _logger = logger;
        _client = new Lazy<StripeClient>(() =>
        {
            if (string.IsNullOrWhiteSpace(options.SecretKey))
            {
                throw new InvalidOperationException("Stripe SecretKey is not configured.");
            }

            return new StripeClient(options.SecretKey);
        });
    }

    public async Task<PaymentIntentData> CreatePaymentIntentAsync(
        long amountCents,
        string currency,
        IReadOnlyDictionary<string, string> metadata,
        string idempotencyKey,
        string statementDescriptorSuffix,
        CancellationToken cancellationToken = default)
    {
        var options = new PaymentIntentCreateOptions
        {
            Amount = amountCents,
            Currency = currency,
            Metadata = new Dictionary<string, string>(metadata),
            StatementDescriptorSuffix = statementDescriptorSuffix,
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions { Enabled = true }
        };

        var requestOptions = new RequestOptions { IdempotencyKey = idempotencyKey };
        var intent = await new PaymentIntentService(_client.Value).CreateAsync(options, requestOptions, cancellationToken);

        _logger.LogInformation("Stripe PaymentIntent {PaymentIntentId} created ({AmountCents} {Currency})", intent.Id, intent.Amount, intent.Currency);

        return new PaymentIntentData(intent.Id, intent.ClientSecret, MapPaymentIntentStatus(intent.Status), intent.Amount, intent.Currency);
    }

    public async Task<PaymentIntentData> RetrievePaymentIntentAsync(
        string paymentIntentId,
        CancellationToken cancellationToken = default)
    {
        var intent = await new PaymentIntentService(_client.Value).GetAsync(paymentIntentId, requestOptions: null, cancellationToken: cancellationToken);

        return new PaymentIntentData(intent.Id, intent.ClientSecret, MapPaymentIntentStatus(intent.Status), intent.Amount, intent.Currency);
    }

    public async Task<PaymentIntentData> CancelPaymentIntentAsync(
        string paymentIntentId,
        CancellationToken cancellationToken = default)
    {
        var intent = await new PaymentIntentService(_client.Value).CancelAsync(paymentIntentId, options: null, requestOptions: null, cancellationToken: cancellationToken);

        _logger.LogInformation("Stripe PaymentIntent {PaymentIntentId} canceled", intent.Id);

        return new PaymentIntentData(intent.Id, intent.ClientSecret, MapPaymentIntentStatus(intent.Status), intent.Amount, intent.Currency);
    }

    public async Task<RefundData> CreateRefundAsync(
        string paymentIntentId,
        long? amountCents,
        string reason,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var options = new RefundCreateOptions
        {
            PaymentIntent = paymentIntentId,
            Amount = amountCents,
            Reason = reason
        };

        var requestOptions = new RequestOptions { IdempotencyKey = idempotencyKey };
        var refund = await new RefundService(_client.Value).CreateAsync(options, requestOptions, cancellationToken);

        _logger.LogInformation("Stripe refund {RefundId} created ({AmountCents}) for PaymentIntent {PaymentIntentId}", refund.Id, refund.Amount, paymentIntentId);

        return new RefundData(refund.Id, refund.Amount, MapRefundStatus(refund.Status));
    }

    public async Task<IReadOnlyList<RefundData>> ListRefundsAsync(
        string paymentIntentId,
        CancellationToken cancellationToken = default)
    {
        var options = new RefundListOptions { PaymentIntent = paymentIntentId };
        var refunds = new List<RefundData>();

        await foreach (var refund in new RefundService(_client.Value).ListAutoPagingAsync(options, requestOptions: null, cancellationToken))
        {
            refunds.Add(new RefundData(refund.Id, refund.Amount, MapRefundStatus(refund.Status)));
        }

        _logger.LogInformation("Stripe listed {RefundCount} refunds for PaymentIntent {PaymentIntentId}", refunds.Count, paymentIntentId);

        return refunds;
    }

    private static PaymentIntentStatus MapPaymentIntentStatus(string? status) => status switch
    {
        "succeeded" => PaymentIntentStatus.Succeeded,
        "processing" => PaymentIntentStatus.Processing,
        "canceled" => PaymentIntentStatus.Canceled,
        "requires_payment_method" or "requires_action" or "requires_confirmation" or "requires_capture" => PaymentIntentStatus.RequiresAction,
        _ => PaymentIntentStatus.Failed
    };

    internal static RefundStatus MapRefundStatus(string? status) => status switch
    {
        "failed" => RefundStatus.Failed,
        "succeeded" => RefundStatus.Succeeded,
        "canceled" => RefundStatus.Canceled,
        _ => RefundStatus.Pending
    };
}
