using System.Text.Json.Serialization;

namespace eNote.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EnrollmentStatus
{
    Active = 1,
    Completed = 2,
    Canceled = 3,
    Pending = 4,
    Rejected = 5
}
