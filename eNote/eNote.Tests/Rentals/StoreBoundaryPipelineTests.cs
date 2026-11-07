using System.Security.Claims;
using eNote.API.Middleware;
using eNote.Application.Common.Persistence;
using eNote.Application.Constants;
using eNote.Application.Features.Rentals.InstrumentRentals;
using eNote.Application.Features.Rentals.InstrumentRentals.Services;
using eNote.Application.Features.Rentals.Instruments;
using eNote.Application.Features.Rentals.Instruments.Services;
using eNote.Tests.TestUtils;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace eNote.Tests.Rentals;

public sealed class StoreBoundaryPipelineTests
{
    private static readonly DateTime Now = new(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Middleware_ResolvesStoreFromEmployee_AndInstrumentServiceStaysInsideIt()
    {
        await using var context = CreateContext();
        var seeded = await SeedAsync(context);
        await RunMiddlewareAsync(context);
        var service = CreateInstrumentService(context, seeded.Actor);

        await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(seeded.StoreBInstrument.Id, new InstrumentRequest
        {
            Model = "Changed",
            ManufacturerId = TestManufacturers.Id("Maker B"),
            InstrumentTypeId = seeded.InstrumentTypeId
        }));
        await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(seeded.StoreBInstrument.Id));

        var stored = await context.Set<Instrument>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleAsync(x => x.Id == seeded.StoreBInstrument.Id);
        Assert.Equal("Store B Model", stored.Model);
        Assert.True(stored.IsActive);

        var paged = await service.GetPagedAsync(new InstrumentSearchObject { PageSize = 10 });

        Assert.Equal([seeded.StoreAInstrument.Id], paged.Items.Select(x => x.Id));
    }

    [Fact]
    public async Task Middleware_ResolvesStoreFromEmployee_AndRentalQueryServiceStaysInsideIt()
    {
        await using var context = CreateContext();
        var seeded = await SeedAsync(context);
        await RunMiddlewareAsync(context);
        var service = new RentalQueryService(
            context,
            TestMapper.Create(),
            seeded.Actor,
            new FixedClock(Now),
            new StubDisplayNameService());

        var paged = await service.GetPagedForStoreAsync(new InstrumentRentalSearchObject { PageSize = 10 });

        Assert.Equal([seeded.StoreARentalId], paged.Items.Select(x => x.Id));
        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdForStoreAsync(seeded.StoreBRentalId));
    }

    private static ENoteContext CreateContext()
    {
        var context = new ENoteContext(TestDbContextFactory.CreateOptions(), new FixedClock(Now), new StubCurrentActor(userId: 10));
        TestManufacturers.Seed(context);

        return context;
    }

    private static async Task<SeededStoreBoundary> SeedAsync(ENoteContext context)
    {
        var storeA = new MusicStore("Store A", "09:00-17:00");
        var storeB = new MusicStore("Store B", "09:00-17:00");
        var type = new InstrumentType { Type = "Guitar", MonthlyFee = 50m };
        context.Set<MusicStore>().AddRange(storeA, storeB);
        context.Set<InstrumentType>().Add(type);
        await context.SaveChangesAsync();

        var employee = new MusicStoreEmployee(appUserId: 10, musicStoreId: storeA.Id, isManager: true);
        var student = new Student(appUserId: 100, enrollmentDate: Now.AddMonths(-1));
        student.UpdateMembership(Now.AddDays(1));
        var instrumentA = new Instrument("Store A Model", TestManufacturers.Id("Maker A"), null, null, type.Id, storeA.Id);
        var instrumentB = new Instrument("Store B Model", TestManufacturers.Id("Maker B"), null, null, type.Id, storeB.Id);
        context.Set<MusicStoreEmployee>().Add(employee);
        context.Set<Student>().Add(student);
        context.Set<Instrument>().AddRange(instrumentA, instrumentB);
        await context.SaveChangesAsync();

        var rentalA = new InstrumentRental(instrumentA.Id, student.Id, storeA.Id, Now, null);
        var rentalB = new InstrumentRental(instrumentB.Id, student.Id, storeB.Id, Now, null);
        context.Set<InstrumentRental>().AddRange(rentalA, rentalB);
        await context.SaveChangesAsync();

        var actor = new StubCurrentActor(userId: 10, employee: employee, storeId: storeA.Id);
        return new SeededStoreBoundary(actor, type.Id, instrumentA, instrumentB, rentalA.Id, rentalB.Id);
    }

    private static async Task RunMiddlewareAsync(ENoteContext context)
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, AppRoles.StoreEmployee)], "TestAuth"))
        };
        var middleware = new TenantInitializationMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(httpContext, context);
    }

    private static InstrumentService CreateInstrumentService(IAppDbContext context, StubCurrentActor actor)
    {
        var rentalCommands = new RentalCommandService(
            context,
            TestMapper.Create(),
            new FixedClock(Now),
            actor,
            actor,
            actor,
            new RecordingNotificationDispatcher(),
            new StubDisplayNameService(),
            new RentalOptions { OverduePenaltyMultiplier = 0.5m });

        return new InstrumentService(context, actor, new StubFileStorageService(), rentalCommands);
    }

    private sealed record SeededStoreBoundary(
        StubCurrentActor Actor,
        int InstrumentTypeId,
        Instrument StoreAInstrument,
        Instrument StoreBInstrument,
        int StoreARentalId,
        int StoreBRentalId);
}
