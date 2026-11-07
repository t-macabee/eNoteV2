using eNote.Application.Common.Localization;
using eNote.Application.Constants;
using eNote.Application.Features.ReferenceData.Addresses;
using eNote.Domain.Entities.Communication;
using eNote.Domain.Entities.Rentals;
using eNote.Tests.TestUtils;
using Microsoft.EntityFrameworkCore;

namespace eNote.Tests.ReferenceData;

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

    [Fact]
    public async Task CreateAsync_ReturnsTheCityName_WhenTheContextHasNotLoadedTheCity()
    {
        var (database, cityId, _) = await SeedAsync();
        await using var ctx = TestDbContextFactory.CreateContext(Now, database);

        var dto = await new AddressService(ctx).CreateAsync(new AddressRequest { CityId = cityId, Street = " Nova ", Number = " 2 " });

        Assert.Equal("Sarajevo", dto.City);
        Assert.Equal("Nova", dto.Street);
        Assert.Equal("2", dto.Number);
        Assert.Equal(cityId, dto.CityId);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsTheCityName_WhenTheContextHasNotLoadedTheCity()
    {
        var (database, cityId, addressId) = await SeedAsync();
        await using var ctx = TestDbContextFactory.CreateContext(Now, database);

        var dto = await new AddressService(ctx).UpdateAsync(addressId, new AddressRequest { CityId = cityId, Street = "Nova", Number = "2" });

        Assert.Equal("Sarajevo", dto.City);
        Assert.Equal("Nova", dto.Street);
        Assert.Equal("2", dto.Number);
    }

    [Fact]
    public async Task CreateAsync_DuplicateAddress_ThrowsConflict()
    {
        var (database, cityId, _) = await SeedAsync();
        await using var ctx = TestDbContextFactory.CreateContext(Now, database);
        var service = new AddressService(ContextFailingOn(ctx, DbConstraintNames.AddressCityStreetNumberUniqueIndex));

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(new AddressRequest { CityId = cityId, Street = "Main St", Number = "1" }));

        Assert.Equal("Adresa s tim gradom, ulicom i brojem već postoji.", exception.Message);
    }

    [Fact]
    public async Task UpdateAsync_DuplicateAddress_ThrowsConflict()
    {
        var (database, cityId, addressId) = await SeedAsync();
        await using var ctx = TestDbContextFactory.CreateContext(Now, database);
        var service = new AddressService(ContextFailingOn(ctx, DbConstraintNames.AddressCityStreetNumberUniqueIndex));

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            service.UpdateAsync(addressId, new AddressRequest { CityId = cityId, Street = "Main St", Number = "1" }));

        Assert.Equal("Adresa s tim gradom, ulicom i brojem već postoji.", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_KeepsTheDatabaseError_ForAnotherIndex()
    {
        var (database, cityId, _) = await SeedAsync();
        await using var ctx = TestDbContextFactory.CreateContext(Now, database);
        var service = new AddressService(ContextFailingOn(ctx, "IX_Other"));

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            service.CreateAsync(new AddressRequest { CityId = cityId, Street = "Nova", Number = "2" }));
    }

    [Fact]
    public async Task DeleteAsync_WhenAnEventUsesTheAddress_ThrowsTheBlockedMessage_AndKeepsTheAddress()
    {
        var (database, _, addressId) = await SeedAsync();
        await using var ctx = TestDbContextFactory.CreateContext(Now, database);
        ctx.Set<Event>().Add(new Event("Koncert", "Opis", Now.AddDays(1), null, addressId));
        await ctx.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<BusinessException>(() => new AddressService(ctx).DeleteAsync(addressId));

        Assert.Equal("Adresa se ne može obrisati jer je povezana s radnjom ili događajem.", exception.Message);
        Assert.True(await ctx.Set<Address>().AnyAsync(x => x.Id == addressId));
    }

    [Fact]
    public async Task DeleteAsync_WhenAStoreUsesTheAddress_ThrowsTheBlockedMessage_AndKeepsTheAddress()
    {
        var (database, _, addressId) = await SeedAsync();
        await using var ctx = TestDbContextFactory.CreateContext(Now, database);
        ctx.Set<MusicStore>().Add(new MusicStore("Radnja", "08-16", addressId));
        await ctx.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<BusinessException>(() => new AddressService(ctx).DeleteAsync(addressId));

        Assert.Equal("Adresa se ne može obrisati jer je povezana s radnjom ili događajem.", exception.Message);
        Assert.True(await ctx.Set<Address>().AnyAsync(x => x.Id == addressId));
    }

    [Fact]
    public async Task DeleteAsync_RemovesAnUnusedAddress()
    {
        var (database, _, addressId) = await SeedAsync();
        await using var ctx = TestDbContextFactory.CreateContext(Now, database);

        await new AddressService(ctx).DeleteAsync(addressId);

        Assert.False(await ctx.Set<Address>().AnyAsync(x => x.Id == addressId));
    }

    [Fact]
    public async Task GetPagedAsync_FiltersByCityAndByStreet_AndOrdersByCityThenStreet()
    {
        var database = Guid.NewGuid().ToString();
        await using var ctx = TestDbContextFactory.CreateContext(Now, database);
        var mostar = new City { Name = "Mostar" };
        var sarajevo = new City { Name = "Sarajevo" };
        ctx.Set<City>().AddRange(sarajevo, mostar);
        await ctx.SaveChangesAsync();
        ctx.Set<Address>().AddRange(
            new Address { CityId = sarajevo.Id, Street = "Bistrik", Number = "1" },
            new Address { CityId = sarajevo.Id, Street = "Alipašina", Number = "2" },
            new Address { CityId = mostar.Id, Street = "Alipašina", Number = "3" });
        await ctx.SaveChangesAsync();
        var service = new AddressService(ctx);

        var all = await service.GetPagedAsync(new AddressSearchObject());
        var byCity = await service.GetPagedAsync(new AddressSearchObject { City = "Saraj" });
        var byStreet = await service.GetPagedAsync(new AddressSearchObject { Street = "Alipaš" });

        Assert.Equal(["3", "2", "1"], all.Items.Select(a => a.Number));
        Assert.Equal(["2", "1"], byCity.Items.Select(a => a.Number));
        Assert.Equal(["3", "2"], byStreet.Items.Select(a => a.Number));
    }

    private static async Task<(string Database, int CityId, int AddressId)> SeedAsync()
    {
        var database = Guid.NewGuid().ToString();
        await using var seed = TestDbContextFactory.CreateContext(Now, database);
        var city = new City { Name = "Sarajevo" };
        seed.Set<City>().Add(city);
        await seed.SaveChangesAsync();
        var address = new Address { CityId = city.Id, Street = "Main St", Number = "1" };
        seed.Set<Address>().Add(address);
        await seed.SaveChangesAsync();
        return (database, city.Id, address.Id);
    }

    private static ThrowingSaveDbContext ContextFailingOn(ENoteContext context, string indexName) =>
        new(context, new DbUpdateException("Save failed.", new Exception($"Cannot insert duplicate key row with unique index '{indexName}'.")));
}
