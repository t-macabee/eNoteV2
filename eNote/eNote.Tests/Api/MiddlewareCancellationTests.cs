using eNote.API.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace eNote.Tests.Api;

// Characterization: UseExceptionHandler itself answers an aborted request with 499 and no body
// before our handler runs, so UseErrorHandling needs no OperationCanceledException arm (B7-05).
public sealed class MiddlewareCancellationTests
{
    [Fact]
    public async Task CancelledRequest_Returns499WithEmptyBody_WithoutOurHandler()
    {
        await using var app = CreateApp();
        await app.StartAsync();

        try
        {
            using var client = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };
            using var response = await client.GetAsync("/cancelled");

            Assert.Equal(499, (int)response.StatusCode);
            Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
        }
        finally
        {
            await app.StopAsync();
        }
    }

    [Fact]
    public async Task NonCancelledOperationCanceledException_FallsThroughTo500()
    {
        await using var app = CreateApp();
        await app.StartAsync();

        try
        {
            using var client = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };
            using var response = await client.GetAsync("/not-cancelled");

            Assert.Equal(500, (int)response.StatusCode);
            Assert.NotEqual(string.Empty, await response.Content.ReadAsStringAsync());
        }
        finally
        {
            await app.StopAsync();
        }
    }

    private static WebApplication CreateApp()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        var app = builder.Build();
        app.UseErrorHandling();
        app.MapGet("/cancelled", (HttpContext ctx) =>
        {
            ctx.RequestAborted = new CancellationToken(canceled: true);
            throw new OperationCanceledException();
        });
        app.MapGet("/not-cancelled", () => { throw new OperationCanceledException(); });
        return app;
    }
}
