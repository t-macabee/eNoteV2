using eNote.Application.Common.Time;

namespace eNote.Tests.TestUtils;

public sealed class FixedClock(DateTime utcNow) : IClock
{
    public DateTime UtcNow => utcNow;
}
