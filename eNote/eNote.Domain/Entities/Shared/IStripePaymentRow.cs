using eNote.Domain.Enums;

namespace eNote.Domain.Entities.Shared;

public interface IStripePaymentRow
{
    string StripePaymentIntentId { get; }
    long AmountChargedCents { get; }
    string Currency { get; }
    PaymentStatus Status { get; }
}
