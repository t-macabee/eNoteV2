using eNote.Application.Common.Paging;

namespace eNote.Tests.Common;

public sealed class PagingLimitsTests
{
    [Theory]
    [InlineData(0, 0, 1, 20)]
    [InlineData(-5, -1, 1, 20)]
    [InlineData(3, 100, 3, 100)]
    [InlineData(1, 101, 1, 100)]
    [InlineData(1, 1000, 1, 100)]
    public void Normalize_ClampsPageAndPageSize(int page, int pageSize, int expectedPage, int expectedPageSize)
    {
        var (normalizedPage, normalizedPageSize) = PagingLimits.Normalize(page, pageSize);

        Assert.Equal(expectedPage, normalizedPage);
        Assert.Equal(expectedPageSize, normalizedPageSize);
    }
}
