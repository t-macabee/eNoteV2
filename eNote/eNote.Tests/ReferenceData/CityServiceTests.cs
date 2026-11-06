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

    private static ThrowingSaveDbContext ContextFailingOn(ENoteContext context, string indexName) =>
        new(context, new DbUpdateException("Save failed.", new Exception($"Cannot insert duplicate key row with unique index '{indexName}'.")));
}
