namespace eNote.Tests.TestUtils;

public sealed class MutableClock(DateTime initial) : IClock
{
    public DateTime UtcNow { get; set; } = initial;
}
