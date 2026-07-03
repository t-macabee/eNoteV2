using eNote.Application.Features.Rentals.InstrumentRentals;
using eNote.Application.Features.Rentals.InstrumentRentals.Billing;
using eNote.Domain.Entities.Rentals;

namespace eNote.Tests.InstrumentRentals;

public sealed class RentalBillingTests
{
    private static readonly DateTime Now = new(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ApplyBilling_BeforePickup_DoesNotCharge()
    {
        var rental = new InstrumentRental(1, 1, 1, Now.AddDays(-2), null);
        var dto = new InstrumentRentalDto();

        RentalBilling.ApplyBilling(rental, dto, Now);

        Assert.Null(dto.TotalFee);
        Assert.Null(dto.MonthsCharged);
        Assert.False(dto.IsProrated);
    }

    [Fact]
    public void ApplyBilling_ActiveRental_ChargesAtLeastOneMonth()
    {
        var rental = new InstrumentRental(1, 1, 1, Now.AddDays(-10), null);
        rental.Approve(50m, null, Now.AddDays(-5), 1);
        rental.Pickup(Now.AddDays(-3));

        var dto = new InstrumentRentalDto();

        RentalBilling.ApplyBilling(rental, dto, Now);

        Assert.Equal(50m, dto.TotalFee);
        Assert.Equal(1, dto.MonthsCharged);
        Assert.False(dto.IsProrated);
    }

    [Fact]
    public void ApplyBilling_ReturnedEarly_UsesProratedDailyFee()
    {
        var rental = new InstrumentRental(1, 1, 1, Now.AddDays(-20), null);
        rental.Approve(30m, null, Now.AddDays(-10), 1);
        rental.Pickup(Now.AddDays(-5));
        rental.ReturnEarly(Now.AddDays(-2), null);

        var dto = new InstrumentRentalDto();

        RentalBilling.ApplyBilling(rental, dto, Now);

        Assert.True(dto.IsProrated);
        Assert.Equal(3, dto.DaysCharged);
        Assert.Equal(1m, dto.DailyFee);
        Assert.Equal(3m, dto.TotalFee);
    }
}