namespace eNote.Application.Features.Payments;

public enum PaymentIntentStatus
{
    RequiresAction = 1,
    Processing = 2,
    Succeeded = 3,
    Canceled = 4,
    Failed = 5
}
