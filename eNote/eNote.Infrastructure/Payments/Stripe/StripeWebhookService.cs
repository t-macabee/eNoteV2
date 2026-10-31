using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Localization;
using eNote.Application.Features.Payments;
using eNote.Application.Features.Payments.Webhooks;
using Microsoft.Extensions.Logging;
using Stripe;
using StripeEvent = Stripe.Event;

namespace eNote.Infrastructure.Payments.Stripe;

public sealed class StripeWebhookService(
    StripeOptions options,
    PaymentWebhookHandler handler,
    ILogger<StripeWebhookService> logger) : IPaymentWebhookReceiver
{
    private const string PaymentIntentSucceeded = "payment_intent.succeeded";
    private const string PaymentIntentPaymentFailed = "payment_intent.payment_failed";
    private const string ChargeRefunded = "charge.refunded";
    private const string ChargeRefundUpdated = "charge.refund.updated";

    public async Task HandleAsync(string rawJson, string signatureHeader, CancellationToken cancellationToken = default)
    {
        StripeEvent stripeEvent;

        try
        {
            stripeEvent = EventUtility.ConstructEvent(rawJson, signatureHeader, options.WebhookSecret, throwOnApiVersionMismatch: false);
        }
        catch (StripeException ex)
        {
            logger.LogWarning(ex, "Stripe webhook signature verification failed");
            throw new BusinessException(Messages.StripeWebhookSignatureInvalid);
        }

        await HandleAsync(stripeEvent, rawJson, cancellationToken);
    }

    public async Task HandleAsync(StripeEvent stripeEvent, string rawJson, CancellationToken cancellationToken = default)
    {
        switch (stripeEvent.Type)
        {
            case PaymentIntentSucceeded when stripeEvent.Data.Object is PaymentIntent paymentIntent:
                await handler.HandlePaymentSucceededAsync(stripeEvent.Id, stripeEvent.Type, paymentIntent.Id, paymentIntent.LatestChargeId, rawJson, cancellationToken);
                break;

            case PaymentIntentPaymentFailed when stripeEvent.Data.Object is PaymentIntent failedIntent:
                await handler.HandlePaymentFailedAsync(stripeEvent.Id, stripeEvent.Type, failedIntent.Id, rawJson, cancellationToken);
                break;

            case ChargeRefunded when stripeEvent.Data.Object is Charge charge:
                await handler.HandleChargeRefundedAsync(stripeEvent.Id, stripeEvent.Type, charge.Id, charge.PaymentIntentId, charge.AmountRefunded, rawJson, cancellationToken);
                break;

            case ChargeRefundUpdated when stripeEvent.Data.Object is Refund refund:
                await handler.HandleRefundUpdatedAsync(
                    stripeEvent.Id,
                    stripeEvent.Type,
                    refund.PaymentIntentId,
                    new RefundData(refund.Id, refund.Amount, StripePaymentGateway.MapRefundStatus(refund.Status)),
                    rawJson,
                    cancellationToken);
                break;

            default:
                await handler.RecordIgnoredEventAsync(stripeEvent.Id, stripeEvent.Type, rawJson, cancellationToken);
                break;
        }
    }
}
