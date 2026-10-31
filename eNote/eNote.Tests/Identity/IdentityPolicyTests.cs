using eNote.Application.Features.Identity.Auth;
using eNote.Application.Validation.Identity;
using eNote.Infrastructure;
using eNote.Tests.TestUtils;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace eNote.Tests.Identity;

public sealed class IdentityPolicyTests
{
    [Fact]
    public void AddInfrastructureIdentity_AppliesIdentityPolicy()
    {
        var services = new ServiceCollection();
        var configuration = TestConfiguration.Create();
        services.AddSingleton<IConfiguration>(configuration);

        services.AddPersistence(configuration).AddInfrastructureIdentity();

        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<IdentityOptions>>().Value;

        Assert.Equal(8, options.Password.RequiredLength);
        Assert.True(options.Password.RequireDigit);
        Assert.True(options.Password.RequireNonAlphanumeric);
        Assert.True(options.Password.RequireUppercase);
        Assert.True(options.Password.RequireLowercase);
        Assert.True(options.Lockout.AllowedForNewUsers);
        Assert.Equal(5, options.Lockout.MaxFailedAccessAttempts);
        Assert.Equal(TimeSpan.FromMinutes(15), options.Lockout.DefaultLockoutTimeSpan);
    }

    [Theory]
    [InlineData("Abcde1!", 1)]
    [InlineData("Abcdef1!", 0)]
    public void PasswordRules_RejectsSevenCharacters_AcceptsEight(string password, int expectedPasswordErrorCount)
    {
        var validator = new RegisterRequestValidator();

        var result = validator.Validate(new RegisterRequest { Username = "jdoe", Email = "jdoe@example.com", Password = password });

        var passwordErrors = result.Errors.Where(e => e.PropertyName == nameof(RegisterRequest.Password)).ToList();
        Assert.Equal(expectedPasswordErrorCount, passwordErrors.Count);
        Assert.All(passwordErrors, e => Assert.Equal("Lozinka mora imati najmanje 8 znakova.", e.ErrorMessage));
    }
}
