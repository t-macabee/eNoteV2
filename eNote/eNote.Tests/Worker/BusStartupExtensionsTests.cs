using eNote.Worker.Extensions;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace eNote.Tests.Worker;

public class BusStartupExtensionsTests
{
    [Fact]
    public void WaitForBusBeforeApplicationStarted_MakesHostWaitForTheBus()
    {
        using var provider = new ServiceCollection()
            .WaitForBusBeforeApplicationStarted()
            .BuildServiceProvider();

        Assert.True(provider.GetRequiredService<IOptions<MassTransitHostOptions>>().Value.WaitUntilStarted);
    }

    [Fact]
    public void MassTransitHostOptions_DoNotWaitForTheBus_ByDefault()
    {
        using var provider = new ServiceCollection().AddOptions().BuildServiceProvider();

        Assert.False(provider.GetRequiredService<IOptions<MassTransitHostOptions>>().Value.WaitUntilStarted);
    }
}
