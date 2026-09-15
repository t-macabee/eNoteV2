using Microsoft.Extensions.Logging;

namespace eNote.Application.Features.Rentals.Payments.Services;

public static class PaymentGatewayHelpers
{
    public static readonly TimeSpan RequiresActionReuseWindow = TimeSpan.FromMinutes(30);

    public static long ToCents(decimal amount) => (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);

    public static string NormalizeCurrency(string currency) => currency.Trim().ToLowerInvariant();

    public static PaymentStatus MapStatus(string? stripeStatus) => stripeStatus switch
    {
        "succeeded" => PaymentStatus.Succeeded,
        "canceled" => PaymentStatus.Canceled,
        "requires_payment_method" or "requires_action" or "requires_confirmation" or "requires_capture" or "processing" => PaymentStatus.RequiresAction,
        _ => PaymentStatus.Failed
    };

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
