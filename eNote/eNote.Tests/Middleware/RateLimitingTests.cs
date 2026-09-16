using eNote.API.Extensions;
using eNote.API.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using System.Net;
using System.Text;
using System.Threading.RateLimiting;

namespace eNote.Tests.Middleware;

public sealed class RateLimitingTests
{
    private static readonly AuthRateLimiterPolicy Policy = new();

    [Fact]
    public async Task InvokeAsync_KeysLoginOnUsername_WhenBodyReadable()
    {
        var context = AuthContext("10.0.0.5", "Login", """{"username":"Alice","password":"secret"}""");

        await InvokePeekAsync(context);

        Assert.Equal("user:alice", Policy.GetPartition(context).PartitionKey);
    }

    [Fact]
    public async Task InvokeAsync_KeysForgotPasswordOnEmail()
    {
        var context = AuthContext("10.0.0.5", "ForgotPassword", """{"email":"Alice@Example.com"}""");

        await InvokePeekAsync(context);

        Assert.Equal("user:alice@example.com", Policy.GetPartition(context).PartitionKey);
    }

    [Fact]
    public async Task InvokeAsync_KeysResetPasswordOnEmail()
    {
        var context = AuthContext("10.0.0.5", "ResetPassword", """{"email":"bob@example.com","token":"t","newPassword":"p"}""");

        await InvokePeekAsync(context);

        Assert.Equal("user:bob@example.com", Policy.GetPartition(context).PartitionKey);
    }

    [Fact]
    public async Task InvokeAsync_KeepsIpKey_ForRegister_EvenWithUsernameBody()
    {
        var context = AuthContext("10.0.0.5", "Register", """{"username":"alice","email":"a@b.c","password":"p"}""");

        await InvokePeekAsync(context);

        Assert.Equal("10.0.0.5", Policy.GetPartition(context).PartitionKey);
    }

    [Fact]
    public async Task InvokeAsync_FallsBackToIp_WhenBodyIsMalformed()
    {
        var context = AuthContext("10.0.0.5", "Login", "not-json");

        await InvokePeekAsync(context);

        Assert.Equal("10.0.0.5", Policy.GetPartition(context).PartitionKey);
    }

    [Fact]
    public async Task InvokeAsync_FallsBackToIp_WhenBodyHasNoUsernameOrEmail()
    {
        var context = AuthContext("10.0.0.5", "Login", """{"other":"value"}""");

        await InvokePeekAsync(context);

        Assert.Equal("10.0.0.5", Policy.GetPartition(context).PartitionKey);
    }

    [Fact]
    public async Task InvokeAsync_FallsBackToIp_WhenBodyExceedsBound()
    {
        var body = "{\"username\":\"" + new string('a', 5000) + "\"}";
        var context = AuthContext("10.0.0.5", "Login", body);

        await InvokePeekAsync(context);

        Assert.Equal("10.0.0.5", Policy.GetPartition(context).PartitionKey);
    }

    [Fact]
    public async Task InvokeAsync_FallsBackToIp_WhenBodyAbsent()
    {
        var context = AuthContext("10.0.0.5", "Login", body: null);

        await InvokePeekAsync(context);

        Assert.Equal("10.0.0.5", Policy.GetPartition(context).PartitionKey);
    }

    [Fact]
    public async Task GetPartitionAsync_KeysOnIdentity_EvenWhenRemoteIpMissing()
    {
        var context = AuthContext(null, "Login", """{"username":"alice"}""");

        await InvokePeekAsync(context);

        Assert.Equal("user:alice", Policy.GetPartition(context).PartitionKey);
    }

    [Fact]
    public async Task InvokeAsync_RewindsBody_AfterRead()
    {
        var context = AuthContext("10.0.0.5", "Login", """{"username":"alice"}""");

        await InvokePeekAsync(context);

        Assert.Equal(0, context.Request.Body.Position);
        using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        Assert.Contains("alice", body);
    }

    [Fact]
    public async Task InvokeAsync_KeysOnUsername_PreferablyOverEmail()
    {
        var context = AuthContext("10.0.0.5", "Login", """{"username":"alice","email":"a@b.c"}""");

        await InvokePeekAsync(context);

        Assert.Equal("user:alice", Policy.GetPartition(context).PartitionKey);
    }

    [Fact]
    public async Task GetPartitionAsync_GivesDistinctKeys_ToDistinctIps()
    {
        var first = AuthContext("10.0.0.5", "Login", body: null);
        var second = AuthContext("10.0.0.6", "Login", body: null);
        await InvokePeekAsync(first);
        await InvokePeekAsync(second);

        Assert.NotEqual(Policy.GetPartition(first).PartitionKey, Policy.GetPartition(second).PartitionKey);
    }

    [Fact]
    public async Task InvokeAsync_GivesIndependentBudgets_ToDifferentUsernames_FromSameIp()
    {
        using var limiter = PartitionedRateLimiter.Create<HttpContext, string>(
            context => Policy.GetPartition(context));

        var alice = AuthContext("10.0.0.5", "Login", """{"username":"alice","password":"p"}""");
        var bob = AuthContext("10.0.0.5", "Login", """{"username":"bob","password":"p"}""");
        await InvokePeekAsync(alice);
        await InvokePeekAsync(bob);

        for (var i = 0; i < 10; i++)
        {
            using var aliceLease = await limiter.AcquireAsync(alice);
            Assert.True(aliceLease.IsAcquired);
        }

        using var bobLease = await limiter.AcquireAsync(bob);
        Assert.True(bobLease.IsAcquired);

        using var rejected = await limiter.AcquireAsync(alice);
        Assert.False(rejected.IsAcquired);
    }

    [Fact]
    public void AuthPartition_KeepsTenPermitLimit()
    {
        var partition = RateLimitingExtensions.AuthPartition("10.0.0.5");
        using var limiter = partition.Factory(partition.PartitionKey);

        for (var i = 0; i < 10; i++)
        {
            using var lease = limiter.AttemptAcquire(1);
            Assert.True(lease.IsAcquired);
        }

        using var rejected = limiter.AttemptAcquire(1);
        Assert.False(rejected.IsAcquired);
    }

    private static async Task InvokePeekAsync(HttpContext context)
    {
        var middleware = new AuthRateLimitIdentityMiddleware(_ => Task.CompletedTask);
        await middleware.InvokeAsync(context);
    }

    private static DefaultHttpContext AuthContext(string? ip, string actionName, string? body)
    {
        var context = new DefaultHttpContext();

        if (ip is not null)
        {
            context.Connection.RemoteIpAddress = IPAddress.Parse(ip);
        }

        context.SetEndpoint(new Endpoint(
            null,
            new EndpointMetadataCollection(new ControllerActionDescriptor
            {
                ControllerName = "Auth",
                ActionName = actionName
            }),
            null));

        if (body is not null)
        {
            var bytes = Encoding.UTF8.GetBytes(body);
            context.Request.ContentType = "application/json";
            context.Request.Body = new MemoryStream(bytes);
            context.Request.ContentLength = bytes.Length;
        }

        return context;
    }
}
