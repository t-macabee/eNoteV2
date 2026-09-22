namespace eNote.Application.Common.Exceptions;

public class PaymentNotYetVisibleException(string? message = null)
    : AppException(503, "error.payment_not_yet_visible", message)
{
}
