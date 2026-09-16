using eNote.API.Extensions;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Threading.RateLimiting;

namespace eNote.Tests.Middleware;

public sealed class RateLimitingTests
{
    [Fact]
    public void AuthPartition_UsesRemoteIpAsPartitionKey()
    {
        var context = ContextFrom("10.0.0.5");

        var partition = RateLimitingExtensions.AuthPartition(context);

        Assert.Equal("10.0.0.5", partition.PartitionKey);
    }

    [Fact]
    public void AuthPartition_GivesDistinctKeys_ToDistinctIps()
    {
        var first = RateLimitingExtensions.AuthPartition(ContextFrom("10.0.0.5"));
        var second = RateLimitingExtensions.AuthPartition(ContextFrom("10.0.0.6"));

        Assert.NotEqual(first.PartitionKey, second.PartitionKey);
    }

    [Fact]
    public void AuthPartition_UsesAnonKey_WhenRemoteIpMissing()
    {
        var partition = RateLimitingExtensions.AuthPartition(new DefaultHttpContext());

        Assert.Equal("anon", partition.PartitionKey);
    }

    [Fact]
    public void AuthPartition_GivesIndependentBudgets_ToDistinctIps()
    {
        using var limiter = PartitionedRateLimiter.Create<HttpContext, string>(RateLimitingExtensions.AuthPartition);
        var first = ContextFrom("10.0.0.5");
        var second = ContextFrom("10.0.0.6");

        for (var i = 0; i < 10; i++)
        {
            using var lease = limiter.AttemptAcquire(first);
            Assert.True(lease.IsAcquired);
        }

        using var other = limiter.AttemptAcquire(second);
        Assert.True(other.IsAcquired);

        using var rejected = limiter.AttemptAcquire(first);
        Assert.False(rejected.IsAcquired);
    }

    [Fact]
    public void AuthPartition_KeepsTenPermitLimit()
    {
        var partition = RateLimitingExtensions.AuthPartition(ContextFrom("10.0.0.5"));
        using var limiter = partition.Factory(partition.PartitionKey);

        for (var i = 0; i < 10; i++)
        {
            using var lease = limiter.AttemptAcquire(1);
            Assert.True(lease.IsAcquired);
        }

        using var rejected = limiter.AttemptAcquire(1);
        Assert.False(rejected.IsAcquired);
    }

    private static DefaultHttpContext ContextFrom(string ip)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(ip);
        return context;
    }
}
