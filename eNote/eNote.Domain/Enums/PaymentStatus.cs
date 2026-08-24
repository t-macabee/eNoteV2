using System.Text.Json.Serialization;

namespace eNote.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PaymentStatus
{
    RequiresAction = 1,
    Succeeded = 2,
    Failed = 3,
    Canceled = 4,
    Refunded = 5,
    PartiallyRefunded = 6
}
