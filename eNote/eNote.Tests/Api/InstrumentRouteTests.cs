using eNote.API.Controllers.Rentals;
using eNote.Application.Constants;
using eNote.Application.Features.Rentals.Instruments.Services;
using eNote.Application.Features.Rentals.Recommendations.Services;
using eNote.Application.Features.Rentals.ReferenceData.InstrumentTypes;
using eNote.Application.Features.Rentals.ReferenceData.Manufacturers;
using eNote.Tests.TestUtils;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace eNote.Tests.Api;

public sealed class InstrumentRouteTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetInstruments_ReturnsOk_ForAStudentToken()
    {
        await using var app = CreateApp();
        await app.StartAsync();

        try
        {
            using var client = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };
            using var response = await client.GetAsync("/api/v1/instruments");

            Assert.Equal(200, (int)response.StatusCode);
        }
        finally
        {
            await app.StopAsync();
        }
    }

    [Fact]
    public async Task GetOldPublicInstrumentsRoute_ReturnsNotFound()
    {
        await using var app = CreateApp();
        await app.StartAsync();

        try
        {
            using var client = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };
            using var response = await client.GetAsync("/api/v1/instruments/public");

            Assert.Equal(404, (int)response.StatusCode);
        }
        finally
        {
            await app.StopAsync();
        }
    }

    private static WebApplication CreateApp()
    {
        var context = TestDbContextFactory.CreateContext(Now);
        var actor = new StubCurrentActor(new Student(50, Now));
        var instrumentService = new InstrumentService(context, actor, new StubFileStorageService(), null!);
        var recommendationService = new RecommendationService(context, null!, actor, actor, new FixedClock(Now), null!);

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        builder.Services.AddControllers().AddApplicationPart(typeof(InstrumentController).Assembly);
        builder.Services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
            options.ApiVersionReader = new Asp.Versioning.UrlSegmentApiVersionReader();
        }).AddMvc();
        builder.Services.AddAuthentication(TestAuthenticationHandler.SchemeName)
            .AddScheme<TestAuthenticationOptions, TestAuthenticationHandler>(TestAuthenticationHandler.SchemeName, options =>
            {
                options.Role = AppRoles.Student;
                options.UserId = 50;
            });
        builder.Services.AddAuthorization();
        builder.Services.AddSingleton(instrumentService);
        builder.Services.AddSingleton(recommendationService);
        builder.Services.AddSingleton(new InstrumentTypeService(context));
        builder.Services.AddSingleton(new ManufacturerService(context));

        var app = builder.Build();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        return app;
    }
}
