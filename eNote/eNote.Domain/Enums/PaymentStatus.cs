namespace eNote.Domain.Enums;

public enum PaymentStatus
{
    RequiresAction = 1,
    Succeeded = 2,
    Failed = 3,
    Canceled = 4,
    Refunded = 5,
    PartiallyRefunded = 6
}
