using System.Text.Json.Serialization;

namespace eNote.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LectureType
{
    Theoretical = 1,
    Practical = 2,
    Combined = 3,
}
