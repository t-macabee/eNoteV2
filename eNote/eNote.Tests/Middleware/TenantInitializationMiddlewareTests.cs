using eNote.API.Middleware;
using eNote.Application.Common.Exceptions;
using eNote.Application.Constants;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace eNote.Tests.Middleware;

public sealed class TenantInitializationMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_SkipsResolution_ForAnonymousRequest()
    {
        var storeContext = new ThrowingStoreContext();
        var nextCalled = false;
        var middleware = new TenantInitializationMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

        await middleware.InvokeAsync(new DefaultHttpContext(), storeContext);

        Assert.True(nextCalled);
        Assert.False(storeContext.Called);
    }

    [Theory]
    [InlineData(AppRoles.Student)]
    [InlineData(AppRoles.Instructor)]
    [InlineData(AppRoles.Administrator)]
    public async Task InvokeAsync_AllowsRequest_WhenStoreNotResolved_ForNonStoreRole(string role)
    {
        var storeContext = new ThrowingStoreContext();
        var nextCalled = false;
        var middleware = new TenantInitializationMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

        await middleware.InvokeAsync(ContextFor(role), storeContext);

        Assert.True(nextCalled);
        Assert.True(storeContext.Called);
    }

    [Fact]
    public async Task InvokeAsync_ThrowsForbidden_WhenStoreNotResolved_ForStoreEmployee()
    {
        var storeContext = new ThrowingStoreContext();
        var nextCalled = false;
        var middleware = new TenantInitializationMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

        await Assert.ThrowsAsync<AuthorizationException>(() =>
            middleware.InvokeAsync(ContextFor(AppRoles.StoreEmployee), storeContext));

        Assert.False(nextCalled);
    }

    private static DefaultHttpContext ContextFor(string role)
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Role, role)], "Bearer");
        return new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
    }

    private sealed class ThrowingStoreContext : IStoreContext
    {
        public bool Called { get; private set; }

        public Task<int> GetCurrentStoreIdAsync(CancellationToken cancellationToken = default)
        {
            Called = true;
            throw new StoreNotResolvedException("active employee store not found");
        }
    }
}
