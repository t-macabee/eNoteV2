using eNote.Application.Features.Rentals.Payments.Services;
using Microsoft.Extensions.Logging;
using Stripe;

namespace eNote.Infrastructure.Payments.Stripe;

public sealed class StripePaymentGateway : IPaymentGateway
{
    private readonly StripeOptions _options;
    private readonly ILogger<StripePaymentGateway> _logger;
    private readonly Lazy<StripeClient> _client;

    public StripePaymentGateway(StripeOptions options, ILogger<StripePaymentGateway> logger)
    {
        _options = options;
        _logger = logger;
        _client = new Lazy<StripeClient>(() =>
        {
            if (string.IsNullOrWhiteSpace(_options.SecretKey))
            {
                throw new InvalidOperationException("Stripe SecretKey is not configured.");
            }

            return new StripeClient(_options.SecretKey);
        });
    }

    public async Task<PaymentIntentData> CreatePaymentIntentAsync(
        long amountCents,
        string currency,
        IReadOnlyDictionary<string, string> metadata,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var options = new PaymentIntentCreateOptions
        {
            Amount = amountCents,
            Currency = currency,
            Metadata = new Dictionary<string, string>(metadata),
            StatementDescriptor = _options.StatementDescriptor,
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions { Enabled = true }
        };

        var requestOptions = new RequestOptions { IdempotencyKey = idempotencyKey };
        var intent = await new PaymentIntentService(_client.Value).CreateAsync(options, requestOptions, cancellationToken);

        _logger.LogInformation("Stripe PaymentIntent {PaymentIntentId} created ({AmountCents} {Currency})", intent.Id, intent.Amount, intent.Currency);

        return new PaymentIntentData(intent.Id, intent.ClientSecret, intent.Status, intent.Amount, intent.Currency);
    }

    public async Task<PaymentIntentData> RetrievePaymentIntentAsync(
        string paymentIntentId,
        CancellationToken cancellationToken = default)
    {
        var intent = await new PaymentIntentService(_client.Value).GetAsync(paymentIntentId, requestOptions: null, cancellationToken: cancellationToken);

        return new PaymentIntentData(intent.Id, intent.ClientSecret, intent.Status, intent.Amount, intent.Currency);
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

        return new RefundData(refund.Id, refund.Amount, refund.Status);
    }
}
