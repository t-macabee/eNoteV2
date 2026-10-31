using eNote.Application.Common.Persistence;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Contracts.Communication;
using eNote.Domain.Entities.Communication;
using eNote.Infrastructure.Data;
using eNote.Tests.TestUtils;
using eNote.Worker;
using eNote.Worker.Extensions;
using eNote.Worker.Rentals;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace eNote.Tests.Worker;

public sealed class RentalPickupExpiryServiceTests
{
    private static readonly DateTime Now = new(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ExpireAsync_RejectsDueRental_ThroughWorkerRegistrations()
    {
        var (provider, rentalId) = await CreateWorkerWithDueRentalAsync();
        await using var _ = provider;

        var service = new RentalPickupExpiryService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<RentalPickupExpiryService>.Instance);

        await service.ExpireAsync(CancellationToken.None);

        using (var scope = provider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ENoteContext>();

            var rental = await context.Set<InstrumentRental>().AsNoTracking().SingleAsync(x => x.Id == rentalId);
            Assert.Equal(InstrumentRentalStatus.Rejected, rental.RentalStatus);
            Assert.Null(rental.RejectedById);

            var outbox = await context.Set<NotificationOutbox>().ToListAsync();
            var row = Assert.Single(outbox);
            Assert.Equal(NotificationMessageTypes.RentalStatusChanged, row.MessageType);
        }
    }

    [Fact]
    public async Task StartAsync_RunsTheSweepOnTheFirstTick()
    {
        var (provider, rentalId) = await CreateWorkerWithDueRentalAsync();
        await using var _ = provider;

        var service = provider.GetServices<IHostedService>().OfType<RentalPickupExpiryService>().Single();

        await service.StartAsync(CancellationToken.None);

        try
        {
            await WaitForRejectedAsync(provider, rentalId);
        }
        finally
        {
            await service.StopAsync(CancellationToken.None);
        }

        using (var scope = provider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ENoteContext>();

            var rental = await context.Set<InstrumentRental>().AsNoTracking().SingleAsync(x => x.Id == rentalId);
            Assert.Equal(InstrumentRentalStatus.Rejected, rental.RentalStatus);
            Assert.Null(rental.RejectedById);
        }
    }

    private static async Task<(ServiceProvider Provider, int RentalId)> CreateWorkerWithDueRentalAsync()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddSingleton(TestDbContextFactory.CreateOptions(dbName));
        services.AddSingleton<IClock>(new FixedClock(Now));
        services.AddScoped<ICurrentUserContext, WorkerActor>();
        services.AddScoped<ENoteContext>();
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<ENoteContext>());
        services.AddScoped<IStoreContext>(sp => sp.GetRequiredService<ENoteContext>());
        services.AddSingleton<IUserIdentityService>(new StubUserIdentityService());
        services.AddSingleton(new eNote.Application.Features.Rentals.InstrumentRentals.RentalOptions { OverduePenaltyMultiplier = 0.5m });
        services.AddRentalPickupExpiry();

        var provider = services.BuildServiceProvider();
        int rentalId;

        using (var scope = provider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ENoteContext>();
            var student = new Student(appUserId: 100, enrollmentDate: Now.AddMonths(-1));
            context.Set<Student>().Add(student);
            await context.SaveChangesAsync();

            var instrument = await RentalTestData.SeedInstrumentAsync(context);
            var rental = new InstrumentRental(instrument.Id, student.Id, instrument.MusicStoreId, Now.AddDays(-4), null);
            RentalTestData.AdvanceTo(rental, InstrumentRentalStatus.Approved, Now, approvedAt: new DateTime(2026, 6, 12, 9, 0, 0, DateTimeKind.Utc));
            context.Set<InstrumentRental>().Add(rental);
            await context.SaveChangesAsync();
            rentalId = rental.Id;
        }

        return (provider, rentalId);
    }

    private static async Task WaitForRejectedAsync(ServiceProvider provider, int rentalId)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);

        while (true)
        {
            using (var scope = provider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ENoteContext>();
                var status = await context.Set<InstrumentRental>().AsNoTracking().Where(x => x.Id == rentalId).Select(x => x.RentalStatus).SingleAsync();

                if (status == InstrumentRentalStatus.Rejected)
                {
                    return;
                }
            }

            if (DateTime.UtcNow >= deadline)
            {
                return;
            }

            await Task.Delay(50);
        }
    }
}
