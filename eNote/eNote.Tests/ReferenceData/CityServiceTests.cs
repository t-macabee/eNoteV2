using eNote.Application.Common.Localization;
using eNote.Application.Constants;
using eNote.Application.Features.ReferenceData.Cities;
using eNote.Tests.TestUtils;
using Microsoft.EntityFrameworkCore;

namespace eNote.Tests.ReferenceData;

public sealed class CityServiceTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CreateAsync_DuplicateName_ThrowsConflict()
    {
        var service = new CityService(ContextFailingOn(TestDbContextFactory.CreateContext(Now), DbConstraintNames.CityNameUniqueIndex));

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(new CityRequest { Name = "Mostar" }));

        Assert.Equal("Grad s tim nazivom već postoji.", exception.Message);
    }

    [Fact]
    public async Task UpdateAsync_DuplicateName_ThrowsConflict()
    {
        var context = TestDbContextFactory.CreateContext(Now);
        var tuzla = await new CityService(context).CreateAsync(new CityRequest { Name = "Tuzla" });
        var service = new CityService(ContextFailingOn(context, DbConstraintNames.CityNameUniqueIndex));

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            service.UpdateAsync(tuzla.Id, new CityRequest { Name = "Mostar" }));

        Assert.Equal("Grad s tim nazivom već postoji.", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_KeepsTheDatabaseError_ForAnotherIndex()
    {
        var service = new CityService(ContextFailingOn(TestDbContextFactory.CreateContext(Now), "IX_Other"));

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            service.CreateAsync(new CityRequest { Name = "Mostar" }));
    }

    [Fact]
    public async Task GetPagedAsync_SetsDeleteBlockedReason_OnlyForACityUsedByAnAddress()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var used = new City { Name = "Sarajevo" };
        var unused = new City { Name = "Mostar" };
        context.Set<City>().AddRange(used, unused);
        await context.SaveChangesAsync();
        context.Set<Address>().Add(new Address { CityId = used.Id, Street = "Main", Number = "1" });
        await context.SaveChangesAsync();
        var service = new CityService(context);

        var page = await service.GetPagedAsync(new CitySearchObject { IncludeTotalCount = true });

        Assert.Equal(Messages.CityDeleteBlocked, page.Items.Single(x => x.Id == used.Id).DeleteBlockedReason);
        Assert.Null(page.Items.Single(x => x.Id == unused.Id).DeleteBlockedReason);
    }

    [Fact]
    public async Task GetByIdAsync_SetsDeleteBlockedReason_ForACityUsedByAnAddress()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var used = new City { Name = "Sarajevo" };
        context.Set<City>().Add(used);
        await context.SaveChangesAsync();
        context.Set<Address>().Add(new Address { CityId = used.Id, Street = "Main", Number = "1" });
        await context.SaveChangesAsync();
        var service = new CityService(context);

        var dto = await service.GetByIdAsync(used.Id);

        Assert.Equal(Messages.CityDeleteBlocked, dto.DeleteBlockedReason);
    }

    private static ThrowingSaveDbContext ContextFailingOn(ENoteContext context, string indexName) =>
        new(context, new DbUpdateException("Save failed.", new Exception($"Cannot insert duplicate key row with unique index '{indexName}'.")));
}
