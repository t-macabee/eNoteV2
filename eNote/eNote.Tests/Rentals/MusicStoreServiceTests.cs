using eNote.Application.Features.Rentals.ReferenceData.MusicStores;
using eNote.Domain.Entities.Shared;
using eNote.Tests.TestUtils;

namespace eNote.Tests.Rentals;

public sealed class MusicStoreServiceTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    // contract: Create round-trips AddressId and returns populated AddressStreet/AddressCity (regression for Create-without-Include pitfall)
    [Fact]
    public async Task CreateAsync_WithAddress_ReturnsPopulatedAddressFields()
    {
        var ctx = TestDbContextFactory.CreateContext(Now);
        var address = await SeedAddressAsync(ctx, "Sarajevo", "Main St");
        var service = new MusicStoreService(ctx);

        var dto = await service.CreateAsync(new MusicStoreRequest
        {
            StoreName = "Music Shop",
            BusinessHours = "09-17",
            AddressId = address.Id
        });

        Assert.Equal(address.Id, dto.AddressId);
        Assert.Equal("Main St", dto.AddressStreet);
        Assert.Equal("Sarajevo", dto.AddressCity);
        Assert.Equal("Music Shop", dto.StoreName);
        Assert.NotEqual(0, dto.Id);
    }

    [Fact]
    public async Task CreateAsync_WithoutAddress_ReturnsNullAddressFields()
    {
        var ctx = TestDbContextFactory.CreateContext(Now);
        var service = new MusicStoreService(ctx);

        var dto = await service.CreateAsync(new MusicStoreRequest
        {
            StoreName = "No Address Shop",
            BusinessHours = "09-17",
            AddressId = null
        });

        Assert.Null(dto.AddressId);
        Assert.Null(dto.AddressStreet);
        Assert.Null(dto.AddressCity);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsPopulatedAddressFields()
    {
        var ctx = TestDbContextFactory.CreateContext(Now);
        var address = await SeedAddressAsync(ctx, "Mostar", "Old Street");
        var service = new MusicStoreService(ctx);

        var created = await service.CreateAsync(new MusicStoreRequest
        {
            StoreName = "Shop A",
            BusinessHours = "09-17",
            AddressId = address.Id
        });

        var fetched = await service.GetByIdAsync(created.Id);

        Assert.Equal(address.Id, fetched.AddressId);
        Assert.Equal("Old Street", fetched.AddressStreet);
        Assert.Equal("Mostar", fetched.AddressCity);
    }

    // contract: Update round-trips AddressId and returns populated AddressStreet/AddressCity
    [Fact]
    public async Task UpdateAsync_ChangesAddress_ReturnsPopulatedFields()
    {
        var ctx = TestDbContextFactory.CreateContext(Now);
        var address1 = await SeedAddressAsync(ctx, "Sarajevo", "Street 1");
        var address2 = await SeedAddressAsync(ctx, "Tuzla", "Street 2");
        var service = new MusicStoreService(ctx);

        var created = await service.CreateAsync(new MusicStoreRequest
        {
            StoreName = "Shop",
            BusinessHours = "09-17",
            AddressId = address1.Id
        });

        var updated = await service.UpdateAsync(created.Id, new MusicStoreRequest
        {
            StoreName = "Shop Updated",
            BusinessHours = "10-18",
            AddressId = address2.Id
        });

        Assert.Equal(address2.Id, updated.AddressId);
        Assert.Equal("Street 2", updated.AddressStreet);
        Assert.Equal("Tuzla", updated.AddressCity);
        Assert.Equal("Shop Updated", updated.StoreName);
    }

    [Fact]
    public async Task UpdateAsync_ClearsAddress_ReturnsNullFields()
    {
        var ctx = TestDbContextFactory.CreateContext(Now);
        var address = await SeedAddressAsync(ctx, "Sarajevo", "Street 1");
        var service = new MusicStoreService(ctx);

        var created = await service.CreateAsync(new MusicStoreRequest
        {
            StoreName = "Shop",
            BusinessHours = "09-17",
            AddressId = address.Id
        });

        var updated = await service.UpdateAsync(created.Id, new MusicStoreRequest
        {
            StoreName = "Shop",
            BusinessHours = "09-17",
            AddressId = null
        });

        Assert.Null(updated.AddressId);
        Assert.Null(updated.AddressStreet);
        Assert.Null(updated.AddressCity);
    }

    [Fact]
    public async Task UpdateAsync_FillsAddressLater_Succeeds()
    {
        var ctx = TestDbContextFactory.CreateContext(Now);
        var address = await SeedAddressAsync(ctx, "Zenica", "Center St");
        var service = new MusicStoreService(ctx);

        var created = await service.CreateAsync(new MusicStoreRequest
        {
            StoreName = "No Address Initially",
            BusinessHours = "09-17",
            AddressId = null
        });

        Assert.Null(created.AddressId);

        var updated = await service.UpdateAsync(created.Id, new MusicStoreRequest
        {
            StoreName = "No Address Initially",
            BusinessHours = "09-17",
            AddressId = address.Id
        });

        Assert.Equal(address.Id, updated.AddressId);
        Assert.Equal("Center St", updated.AddressStreet);
        Assert.Equal("Zenica", updated.AddressCity);
    }

    // contract: GetPagedAsync filters by CityId correctly
    [Fact]
    public async Task GetPagedAsync_FiltersByCityId()
    {
        var ctx = TestDbContextFactory.CreateContext(Now);
        var citySarajevo = await SeedCityAsync(ctx, "Sarajevo");
        var cityMostar = await SeedCityAsync(ctx, "Mostar");
        var addrSarajevo = await SeedAddressWithCityAsync(ctx, citySarajevo, "Street A");
        var addrMostar = await SeedAddressWithCityAsync(ctx, cityMostar, "Street B");
        var service = new MusicStoreService(ctx);

        await service.CreateAsync(new MusicStoreRequest { StoreName = "Shop Sarajevo 1", BusinessHours = "09-17", AddressId = addrSarajevo.Id });
        await service.CreateAsync(new MusicStoreRequest { StoreName = "Shop Sarajevo 2", BusinessHours = "09-17", AddressId = addrSarajevo.Id });
        await service.CreateAsync(new MusicStoreRequest { StoreName = "Shop Mostar", BusinessHours = "09-17", AddressId = addrMostar.Id });
        await service.CreateAsync(new MusicStoreRequest { StoreName = "Shop No Address", BusinessHours = "09-17", AddressId = null });

        var filtered = await service.GetPagedAsync(new MusicStoreSearchObject { CityId = citySarajevo.Id });
        Assert.Equal(2, filtered.Items.Count);
        Assert.All(filtered.Items, x => Assert.Equal("Sarajevo", x.AddressCity));

        var filteredMostar = await service.GetPagedAsync(new MusicStoreSearchObject { CityId = cityMostar.Id });
        Assert.Single(filteredMostar.Items);
        Assert.Equal("Mostar", filteredMostar.Items[0].AddressCity);

        var noFilter = await service.GetPagedAsync(new MusicStoreSearchObject());
        Assert.Equal(4, noFilter.Items.Count);
    }

    [Fact]
    public async Task GetPagedAsync_FilterByCityId_ExcludesStoresWithoutAddress()
    {
        var ctx = TestDbContextFactory.CreateContext(Now);
        var city = await SeedCityAsync(ctx, "Sarajevo");
        var addr = await SeedAddressWithCityAsync(ctx, city, "Street A");
        var service = new MusicStoreService(ctx);

        await service.CreateAsync(new MusicStoreRequest { StoreName = "With Address", BusinessHours = "09-17", AddressId = addr.Id });
        await service.CreateAsync(new MusicStoreRequest { StoreName = "Without Address", BusinessHours = "09-17", AddressId = null });

        var filtered = await service.GetPagedAsync(new MusicStoreSearchObject { CityId = city.Id });
        Assert.Single(filtered.Items);
        Assert.Equal("With Address", filtered.Items[0].StoreName);
    }

    // characterization: existing StoreName search filter still works unchanged
    [Fact]
    public async Task GetPagedAsync_FiltersByStoreName_StillWorks()
    {
        var ctx = TestDbContextFactory.CreateContext(Now);
        var service = new MusicStoreService(ctx);

        await service.CreateAsync(new MusicStoreRequest { StoreName = "Music Shop Alpha", BusinessHours = "09-17" });
        await service.CreateAsync(new MusicStoreRequest { StoreName = "Music Shop Beta", BusinessHours = "09-17" });
        await service.CreateAsync(new MusicStoreRequest { StoreName = "Guitar Center", BusinessHours = "09-17" });

        var byName = await service.GetPagedAsync(new MusicStoreSearchObject { StoreName = "Music Shop" });
        Assert.Equal(2, byName.Items.Count);
        Assert.All(byName.Items, x => Assert.Contains("Music Shop", x.StoreName));

        var single = await service.GetPagedAsync(new MusicStoreSearchObject { StoreName = "Alpha" });
        Assert.Single(single.Items);
        Assert.Equal("Music Shop Alpha", single.Items[0].StoreName);
    }

    [Fact]
    public async Task GetPagedAsync_FiltersByStoreNameAndCityId_Combined()
    {
        var ctx = TestDbContextFactory.CreateContext(Now);
        var citySarajevo = await SeedCityAsync(ctx, "Sarajevo");
        var cityMostar = await SeedCityAsync(ctx, "Mostar");
        var addrSarajevo = await SeedAddressWithCityAsync(ctx, citySarajevo, "Street A");
        var addrMostar = await SeedAddressWithCityAsync(ctx, cityMostar, "Street B");
        var service = new MusicStoreService(ctx);

        await service.CreateAsync(new MusicStoreRequest { StoreName = "Music Shop Sarajevo", BusinessHours = "09-17", AddressId = addrSarajevo.Id });
        await service.CreateAsync(new MusicStoreRequest { StoreName = "Music Shop Mostar", BusinessHours = "09-17", AddressId = addrMostar.Id });
        await service.CreateAsync(new MusicStoreRequest { StoreName = "Guitar Shop Sarajevo", BusinessHours = "09-17", AddressId = addrSarajevo.Id });

        var filtered = await service.GetPagedAsync(new MusicStoreSearchObject { StoreName = "Music Shop", CityId = citySarajevo.Id });
        Assert.Single(filtered.Items);
        Assert.Equal("Music Shop Sarajevo", filtered.Items[0].StoreName);
    }

    private static async Task<City> SeedCityAsync(ENoteContext ctx, string name)
    {
        var city = new City { Name = name };
        ctx.Set<City>().Add(city);
        await ctx.SaveChangesAsync();
        return city;
    }

    private static async Task<Address> SeedAddressWithCityAsync(ENoteContext ctx, City city, string street)
    {
        var address = new Address { CityId = city.Id, Street = street, Number = "1" };
        ctx.Set<Address>().Add(address);
        await ctx.SaveChangesAsync();
        return address;
    }

    private static async Task<Address> SeedAddressAsync(ENoteContext ctx, string cityName, string street)
    {
        var city = new City { Name = cityName };
        ctx.Set<City>().Add(city);
        await ctx.SaveChangesAsync();

        var address = new Address { CityId = city.Id, Street = street, Number = "1" };
        ctx.Set<Address>().Add(address);
        await ctx.SaveChangesAsync();
        return address;
    }
}
