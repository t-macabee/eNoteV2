using eNote.Application.Common.Localization;
using eNote.Application.Features.Rentals.ReferenceData.Addresses;
using eNote.Domain.Entities.Shared;
using eNote.Tests.TestUtils;
using Microsoft.EntityFrameworkCore;

namespace eNote.Tests.Rentals;

public sealed class AddressServiceTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CreateAsync_Throws_WhenCityMissing()
    {
        var ctx = TestDbContextFactory.CreateContext(Now);
        var service = new AddressService(ctx);

        var ex = await Assert.ThrowsAsync<BusinessException>(() => service.CreateAsync(new AddressRequest
        {
            CityId = 999,
            Street = "Main St",
            Number = "1"
        }));

        Assert.Equal(Messages.CityNotFound, ex.Message);
    }

    [Fact]
    public async Task UpdateAsync_Throws_WhenCityMissing()
    {
        var ctx = TestDbContextFactory.CreateContext(Now);
        var city = new City { Name = "Sarajevo" };
        ctx.Set<City>().Add(city);
        await ctx.SaveChangesAsync();
        var address = new Address { CityId = city.Id, Street = "Main St", Number = "1" };
        ctx.Set<Address>().Add(address);
        await ctx.SaveChangesAsync();
        var service = new AddressService(ctx);

        var ex = await Assert.ThrowsAsync<BusinessException>(() => service.UpdateAsync(address.Id, new AddressRequest
        {
            CityId = 999,
            Street = "Other St",
            Number = "2"
        }));

        Assert.Equal(Messages.CityNotFound, ex.Message);
    }
}
