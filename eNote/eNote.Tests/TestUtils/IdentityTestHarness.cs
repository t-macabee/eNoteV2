using eNote.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace eNote.Tests.TestUtils;

public sealed record IdentityServices(
    UserManager<AppUser> UserManager,
    RoleManager<AppRole> RoleManager,
    SignInManager<AppUser> SignInManager);

public static class IdentityTestHarness
{
    /// <summary>
    /// Builds the ASP.NET Core Identity graph (UserManager, RoleManager, SignInManager, token
    /// providers) the same way the production API does, over the supplied in-memory context.
    /// </summary>
    public static IdentityServices Create(ENoteContext context)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();
        services.AddDataProtection();
        services.AddHttpContextAccessor();
        services.AddScoped<ENoteContext>(_ => context);
        services.AddScoped<IAuthenticationSchemeProvider>(_ => new StubAuthenticationSchemeProvider());
        services.AddScoped<IUserConfirmation<AppUser>>(_ => new ConfirmedUserConfirmation());

        services.AddIdentityCore<AppUser>(options => { })
            .AddRoles<AppRole>()
            .AddEntityFrameworkStores<ENoteContext>()
            .AddSignInManager<SignInManager<AppUser>>()
            .AddDefaultTokenProviders();

        var provider = services.BuildServiceProvider();

        return new IdentityServices(
            provider.GetRequiredService<UserManager<AppUser>>(),
            provider.GetRequiredService<RoleManager<AppRole>>(),
            provider.GetRequiredService<SignInManager<AppUser>>());
    }

    private sealed class ConfirmedUserConfirmation : IUserConfirmation<AppUser>
    {
        public Task<bool> IsConfirmedAsync(UserManager<AppUser> manager, AppUser user) => Task.FromResult(true);
    }

    private sealed class StubAuthenticationSchemeProvider : IAuthenticationSchemeProvider
    {
        public Task<AuthenticationScheme?> GetDefaultAuthenticateSchemeAsync() => Task.FromResult<AuthenticationScheme?>(null);
        public Task<AuthenticationScheme?> GetDefaultChallengeSchemeAsync() => Task.FromResult<AuthenticationScheme?>(null);
        public Task<AuthenticationScheme?> GetDefaultForbidSchemeAsync() => Task.FromResult<AuthenticationScheme?>(null);
        public Task<AuthenticationScheme?> GetDefaultSignInSchemeAsync() => Task.FromResult<AuthenticationScheme?>(null);
        public Task<AuthenticationScheme?> GetDefaultSignOutSchemeAsync() => Task.FromResult<AuthenticationScheme?>(null);
        public Task<IEnumerable<AuthenticationScheme>> GetAllSchemesAsync() => Task.FromResult<IEnumerable<AuthenticationScheme>>([]);
        public Task<AuthenticationScheme?> GetSchemeAsync(string name) => Task.FromResult<AuthenticationScheme?>(null);
        public Task<IEnumerable<AuthenticationScheme>> GetRequestHandlerSchemesAsync() => Task.FromResult<IEnumerable<AuthenticationScheme>>([]);
        public void AddScheme(AuthenticationScheme scheme) => throw new NotSupportedException();
        public void RemoveScheme(string name) => throw new NotSupportedException();
    }
}
