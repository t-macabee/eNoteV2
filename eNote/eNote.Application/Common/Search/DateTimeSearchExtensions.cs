namespace eNote.Application.Common.Search;

public static class DateTimeSearchExtensions
{
    public static DateTime? ToUtc(this DateTime? value) => value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : null;
}
