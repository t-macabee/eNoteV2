using Microsoft.Extensions.Logging;

namespace eNote.Application.Features.Rentals.Payments.Services;

public sealed record PaymentIntentCreation(
    long AmountCents,
    string Currency,
    IReadOnlyDictionary<string, string> Metadata,
    string IdempotencyKey,
    string StatementDescriptor);

public sealed record ExpectedPayment(long AmountCents, string Currency);

public sealed record PaymentConstraintNames(string PaymentIntentId, string OpenAttempt);

public static class PaymentGatewayHelpers
{
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
        string ownerLabel,
        ExpectedPayment expected,
        IQueryable<TPayment> openAttempts,
        Func<Task<PaymentIntentCreation>> creationFactory,
        Func<PaymentIntentData, TPayment> rowFactory,
        PaymentConstraintNames constraints,
        CancellationToken cancellationToken)
        where TPayment : StripePayment
    {
        var rows = await openAttempts.ToListAsync(cancellationToken);

        var reusable = await ResolveOpenAttemptsAsync(
            logger,
            context,
            paymentGateway,
            rows,
            expected,
            Messages.PaymentProcessing,
            cancellationToken);

        if (reusable is not null)
        {
            logger.LogInformation("Reusing PaymentIntent {PaymentIntentId} for {Owner}", reusable.Id, ownerLabel);

            return (reusable.Id, reusable.ClientSecret, reusable.AmountCents, reusable.Currency, MapStatus(reusable.Status));
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
        catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex, constraints.PaymentIntentId))
        {
            logger.LogInformation("PaymentIntent {PaymentIntentId} for {Owner} was saved by a parallel request", intent.Id, ownerLabel);

            var winner = await context.Set<TPayment>()
                .AsNoTracking()
                .FirstAsync(p => p.StripePaymentIntentId == intent.Id, cancellationToken);

            return (winner.StripePaymentIntentId, intent.ClientSecret, winner.AmountChargedCents, winner.Currency, winner.Status);
        }
        catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex, constraints.OpenAttempt))
        {
            logger.LogInformation("PaymentIntent {PaymentIntentId} for {Owner} lost the open-attempt race and is canceled", intent.Id, ownerLabel);

            await InvokeGatewayAsync(logger, () => paymentGateway.CancelPaymentIntentAsync(intent.Id, cancellationToken));

            var winner = await openAttempts.AsNoTracking().FirstAsync(cancellationToken);
            var current = await InvokeGatewayAsync(logger, () => paymentGateway.RetrievePaymentIntentAsync(winner.StripePaymentIntentId, cancellationToken));

            return (winner.StripePaymentIntentId, current.ClientSecret, winner.AmountChargedCents, winner.Currency, winner.Status);
        }

        logger.LogInformation("Created PaymentIntent {PaymentIntentId} for {Owner} ({AmountCents} {Currency})", intent.Id, ownerLabel, intent.AmountCents, intent.Currency);

        return (intent.Id, intent.ClientSecret, intent.AmountCents, intent.Currency, payment.Status);
    }

    public static async Task<PaymentIntentData?> ResolveOpenAttemptsAsync<TPayment>(
        ILogger logger,
        IAppDbContext context,
        IPaymentGateway paymentGateway,
        IReadOnlyList<TPayment> openAttempts,
        ExpectedPayment? expected,
        string blockedMessage,
        CancellationToken cancellationToken)
        where TPayment : StripePayment
    {
        PaymentIntentData? reusable = null;
        var changed = false;

        foreach (var row in openAttempts)
        {
            var intent = await InvokeGatewayAsync(
                logger,
                () => paymentGateway.RetrievePaymentIntentAsync(row.StripePaymentIntentId, cancellationToken));

            if (intent.Status is "succeeded" or "processing")
            {
                throw new BusinessException(blockedMessage);
            }

            if (intent.Status == "canceled")
            {
                row.MarkCanceled();
                changed = true;
                continue;
            }

            if (reusable is null && expected is not null && Matches(intent, expected))
            {
                reusable = intent;
                continue;
            }

            await CancelOpenAttemptAsync(logger, paymentGateway, row.StripePaymentIntentId, blockedMessage, cancellationToken);
            row.MarkCanceled();
            changed = true;
        }

        if (changed)
        {
            try
            {
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConflictException(Messages.ConcurrencyConflict);
            }
        }

        return reusable;
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

    private static bool Matches(PaymentIntentData intent, ExpectedPayment expected) =>
        intent.AmountCents == expected.AmountCents
        && string.Equals(intent.Currency, expected.Currency, StringComparison.OrdinalIgnoreCase);

    private static async Task CancelOpenAttemptAsync(
        ILogger logger,
        IPaymentGateway paymentGateway,
        string paymentIntentId,
        string blockedMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            await InvokeGatewayAsync(logger, () => paymentGateway.CancelPaymentIntentAsync(paymentIntentId, cancellationToken));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var current = await InvokeGatewayAsync(logger, () => paymentGateway.RetrievePaymentIntentAsync(paymentIntentId, cancellationToken));

            if (current.Status is "succeeded" or "processing")
            {
                throw new BusinessException(blockedMessage);
            }

            throw;
        }
    }
}
