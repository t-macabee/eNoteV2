using Microsoft.Extensions.Logging;

namespace eNote.Application.Features.Rentals.Payments.Services;

public sealed record PaymentIntentCreation(
    long AmountCents,
    string Currency,
    IReadOnlyDictionary<string, string> Metadata,
    string IdempotencyKey,
    string StatementDescriptor);

public static class PaymentGatewayHelpers
{
    public static readonly TimeSpan RequiresActionReuseWindow = TimeSpan.FromMinutes(30);

    public static long ToCents(decimal amount) => (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);

    public static PaymentStatus MapStatus(string? stripeStatus) => stripeStatus switch
    {
        "succeeded" => PaymentStatus.Succeeded,
        "canceled" => PaymentStatus.Canceled,
        "requires_payment_method" or "requires_action" or "requires_confirmation" or "requires_capture" or "processing" => PaymentStatus.RequiresAction,
        _ => PaymentStatus.Failed
    };

    public static async Task<(string PaymentIntentId, string ClientSecret, long AmountCents, string Currency, PaymentStatus Status)> ReuseOrCreatePaymentIntentAsync<TPayment>(
        ILogger logger,
        IAppDbContext context,
        IPaymentGateway paymentGateway,
        int ownerId,
        string ownerKind,
        IQueryable<TPayment> existingRows,
        Func<TPayment, string> intentIdSelector,
        Func<TPayment, (long AmountCents, string Currency, PaymentStatus Status)> rowStateSelector,
        Func<Task<PaymentIntentCreation>> creationFactory,
        Func<PaymentIntentData, TPayment> rowFactory,
        string uniqueConstraintName,
        Func<string, Task<TPayment>> winnerLookup,
        CancellationToken cancellationToken)
        where TPayment : class
    {
        var existing = await existingRows.FirstOrDefaultAsync(cancellationToken);

        if (existing is not null)
        {
            var existingIntentId = intentIdSelector(existing);
            logger.LogInformation("Reusing requires-action PaymentIntent {PaymentIntentId} for {OwnerKind} {OwnerId}", existingIntentId, ownerKind, ownerId);

            var current = await InvokeGatewayAsync(
                logger,
                () => paymentGateway.RetrievePaymentIntentAsync(existingIntentId, cancellationToken));

            return (current.Id, current.ClientSecret, current.AmountCents, current.Currency, MapStatus(current.Status));
        }

        var creation = await creationFactory();

        var intent = await InvokeGatewayAsync(
            logger,
            () => paymentGateway.CreatePaymentIntentAsync(
                creation.AmountCents,
                creation.Currency,
                creation.Metadata,
                creation.IdempotencyKey,
                creation.StatementDescriptor,
                cancellationToken));

        var payment = rowFactory(intent);
        context.Set<TPayment>().Add(payment);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains(uniqueConstraintName) == true)
        {
            var winner = await winnerLookup(intent.Id);
            var (winnerAmountCents, winnerCurrency, winnerStatus) = rowStateSelector(winner);
            return (intentIdSelector(winner), intent.ClientSecret, winnerAmountCents, winnerCurrency, winnerStatus);
        }

        logger.LogInformation("Created PaymentIntent {PaymentIntentId} for {OwnerKind} {OwnerId} ({AmountCents} {Currency})", intent.Id, ownerKind, ownerId, intent.AmountCents, intent.Currency);

        return (intent.Id, intent.ClientSecret, intent.AmountCents, intent.Currency, rowStateSelector(payment).Status);
    }

    public static async Task<T> InvokeGatewayAsync<T>(ILogger logger, Func<Task<T>> call)
    {
        try
        {
            return await call();
        }
        catch (Exception ex) when (ex is not AppException and not OperationCanceledException)
        {
            logger.LogError(ex, "Stripe gateway invocation failed: {Message}", ex.Message);
            throw new PaymentProviderUnavailableException(Messages.PaymentProviderUnavailable);
        }
    }
}
