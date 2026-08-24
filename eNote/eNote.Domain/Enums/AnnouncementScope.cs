using System.Text.Json.Serialization;

namespace eNote.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AnnouncementScope
{
    Course = 1,
    MusicStore = 2
}
