using eNote.Application.Features.Rentals.InstrumentRentals.Services;

namespace eNote.Worker.Rentals;

public sealed class RentalPickupExpiryService(IServiceScopeFactory scopeFactory, ILogger<RentalPickupExpiryService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        do
        {
            try
            {
                await ExpireAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Rental pickup expiry failed; retrying next tick.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    internal async Task ExpireAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();

        var service = scope.ServiceProvider.GetRequiredService<RentalCommandService>();
        var count = await service.ExpireUnclaimedApprovalsAsync(cancellationToken);

        if (count > 0)
        {
            logger.LogInformation("Rejected {Count} approved rentals that were not picked up in time.", count);
        }
    }
}
