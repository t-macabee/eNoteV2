using eNote.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace eNote.Tests.Identity;

public sealed class IdentityOptionsTests
{
    private static IdentityOptions AppOptions()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructureIdentity();
        using var provider = services.BuildServiceProvider();

        return provider.GetRequiredService<IOptions<IdentityOptions>>().Value;
    }

    [Fact]
    public void AppOptions_RequireAUniqueEmail()
    {
        Assert.True(AppOptions().User.RequireUniqueEmail);
    }

    [Theory]
    [InlineData('č')]
    [InlineData('ć')]
    [InlineData('š')]
    [InlineData('ž')]
    [InlineData('đ')]
    [InlineData('Č')]
    [InlineData('Ć')]
    [InlineData('Š')]
    [InlineData('Ž')]
    [InlineData('Đ')]
    [InlineData('a')]
    [InlineData('Z')]
    [InlineData('0')]
    [InlineData('-')]
    [InlineData('.')]
    [InlineData('_')]
    [InlineData('@')]
    [InlineData('+')]
    public void AppOptions_AllowTheUserNameCharacter(char character)
    {
        Assert.Contains(character, AppOptions().User.AllowedUserNameCharacters);
    }

    [Fact]
    public void AppOptions_KeepTheLockoutPolicy()
    {
        var lockout = AppOptions().Lockout;

        Assert.Equal(5, lockout.MaxFailedAccessAttempts);
        Assert.Equal(TimeSpan.FromMinutes(15), lockout.DefaultLockoutTimeSpan);
    }
}
