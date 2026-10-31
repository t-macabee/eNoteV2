namespace eNote.Application.Common.Exceptions;

public class PaymentProviderUnavailableException(string? message = null)
    : AppException(503, "error.payment_provider_unavailable", message)
{
}
