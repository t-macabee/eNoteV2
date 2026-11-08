using eNote.Application.Common.Files;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Persistence;
using eNote.Application.Features.Rentals.InstrumentRentals;
using eNote.Application.Features.Rentals.InstrumentRentals.Services;
using eNote.Application.Features.Rentals.Instruments;
using eNote.Application.Features.Rentals.Instruments.Services;
using eNote.Tests.TestUtils;
using Microsoft.EntityFrameworkCore;

namespace eNote.Tests.Rentals;

public sealed class InstrumentServiceTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetPagedAsync_FiltersByMusicStoreId()
    {
        var ctx = CreateContext();
        var type = await SeedInstrumentTypeAsync(ctx, "Guitar");
        var store1 = await SeedStoreAsync(ctx, "Music Shop Sarajevo");
        var store2 = await SeedStoreAsync(ctx, "Music Shop Mostar");

        await SeedInstrumentAsync(ctx, store1.Id, type.Id, "Stratocaster", "Fender");
        await SeedInstrumentAsync(ctx, store1.Id, type.Id, "Telecaster", "Fender");
        await SeedInstrumentAsync(ctx, store2.Id, type.Id, "Les Paul", "Gibson");

        var service = CreateInstrumentService(ctx);

        var filteredStore1 = await service.GetPagedAsync(new InstrumentSearchObject { MusicStoreId = store1.Id });
        Assert.Equal(2, filteredStore1.Items.Count);
        Assert.All(filteredStore1.Items, x => Assert.Equal("Music Shop Sarajevo", x.MusicStore));

        var filteredStore2 = await service.GetPagedAsync(new InstrumentSearchObject { MusicStoreId = store2.Id });
        Assert.Single(filteredStore2.Items);
        Assert.Equal("Les Paul", filteredStore2.Items[0].Model);
        Assert.Equal("Music Shop Mostar", filteredStore2.Items[0].MusicStore);

        var noFilter = await service.GetPagedAsync(new InstrumentSearchObject());
        Assert.Equal(3, noFilter.Items.Count);
    }

    [Fact]
    public async Task GetPagedAsync_FiltersByModelAndMusicStoreId_Combined()
    {
        var ctx = CreateContext();
        var type = await SeedInstrumentTypeAsync(ctx, "Guitar");
        var store1 = await SeedStoreAsync(ctx, "Music Shop Sarajevo");
        var store2 = await SeedStoreAsync(ctx, "Music Shop Mostar");

        await SeedInstrumentAsync(ctx, store1.Id, type.Id, "Yamaha Pacifica", "Yamaha");
        await SeedInstrumentAsync(ctx, store1.Id, type.Id, "Fender Stratocaster", "Fender");
        await SeedInstrumentAsync(ctx, store2.Id, type.Id, "Yamaha Pacifica", "Yamaha");

        var service = CreateInstrumentService(ctx);

        var filtered = await service.GetPagedAsync(new InstrumentSearchObject
        {
            Model = "Pacifica",
            MusicStoreId = store1.Id
        });

        Assert.Single(filtered.Items);
        Assert.Equal("Music Shop Sarajevo", filtered.Items[0].MusicStore);
        Assert.Equal("Yamaha Pacifica", filtered.Items[0].Model);
    }

    [Fact]
    public async Task GetPagedAsync_FiltersBySearch_MatchesModelOrManufacturer()
    {
        var ctx = CreateContext();
        var type = await SeedInstrumentTypeAsync(ctx, "Guitar");
        var store = await SeedStoreAsync(ctx, "Music Shop Sarajevo");

        await SeedInstrumentAsync(ctx, store.Id, type.Id, "Pacifica 112V", "Yamaha");
        await SeedInstrumentAsync(ctx, store.Id, type.Id, "Stratocaster", "Fender");
        await SeedInstrumentAsync(ctx, store.Id, type.Id, "Les Paul", "Gibson");

        var service = CreateInstrumentService(ctx);

        var modelMatch = await service.GetPagedAsync(new InstrumentSearchObject
        {
            Search = "Stratocaster"
        });
        Assert.Single(modelMatch.Items);
        Assert.Equal("Stratocaster", modelMatch.Items[0].Model);

        var mfrMatch = await service.GetPagedAsync(new InstrumentSearchObject
        {
            Search = "Yamaha"
        });
        Assert.Single(mfrMatch.Items);
        Assert.Equal("Pacifica 112V", mfrMatch.Items[0].Model);
        Assert.Equal("Yamaha", mfrMatch.Items[0].ManufacturerName);
    }


    [Fact]
    public async Task GetPagedAsync_FiltersByManufacturerAndInstrumentTypeId()
    {
        var ctx = CreateContext();
        var guitar = await SeedInstrumentTypeAsync(ctx, "Guitar");
        var piano = await SeedInstrumentTypeAsync(ctx, "Piano");
        var store = await SeedStoreAsync(ctx, "Music Shop Sarajevo");

        await SeedInstrumentAsync(ctx, store.Id, guitar.Id, "Stratocaster", "Fender");
        await SeedInstrumentAsync(ctx, store.Id, guitar.Id, "Les Paul", "Gibson");
        await SeedInstrumentAsync(ctx, store.Id, piano.Id, "Yamaha P-125", "Yamaha");

        var service = CreateInstrumentService(ctx);

        var byManufacturer = await service.GetPagedAsync(new InstrumentSearchObject { ManufacturerId = TestManufacturers.Id("Fender") });
        var manufacturerItem = Assert.Single(byManufacturer.Items);
        Assert.Equal("Stratocaster", manufacturerItem.Model);

        var byType = await service.GetPagedAsync(new InstrumentSearchObject { InstrumentTypeId = guitar.Id });
        Assert.Equal(2, byType.Items.Count);
        Assert.All(byType.Items, x => Assert.Equal("Guitar", x.InstrumentType));
    }

    [Fact]
    public async Task GetPagedAsync_OrdersNewestInstrumentFirst()
    {
        var ctx = CreateContext();
        var type = await SeedInstrumentTypeAsync(ctx, "Guitar");
        var store = await SeedStoreAsync(ctx, "Music Shop Sarajevo");

        var oldest = await SeedInstrumentAsync(ctx, store.Id, type.Id, "Pacifica", "Yamaha");
        var middle = await SeedInstrumentAsync(ctx, store.Id, type.Id, "Stratocaster", "Fender");
        var newest = await SeedInstrumentAsync(ctx, store.Id, type.Id, "Les Paul", "Gibson");

        var service = CreateInstrumentService(ctx);

        var paged = await service.GetPagedAsync(new InstrumentSearchObject());

        Assert.Equal([newest.Id, middle.Id, oldest.Id], paged.Items.Select(x => x.Id));
    }

    [Fact]
    public async Task MonthlyFee_ComesFromInstrumentType_InDetailAndList()
    {
        var ctx = CreateContext();
        var type = await SeedInstrumentTypeAsync(ctx, "Guitar", 45m);
        var store = await SeedStoreAsync(ctx, "Music Shop Sarajevo");
        var instrument = await SeedInstrumentAsync(ctx, store.Id, type.Id, "Stratocaster", "Fender");

        var service = CreateInstrumentService(ctx);

        var detail = await service.GetByIdAsync(instrument.Id);
        var list = await service.GetPagedAsync(new InstrumentSearchObject());

        Assert.Equal(45m, detail.MonthlyFee);
        Assert.Equal(45m, Assert.Single(list.Items).MonthlyFee);
    }

    [Fact]
    public async Task CreateAsync_StoresEmployeeStoreId_AndTrimsFields()
    {
        var ctx = CreateContext();
        var type = await SeedInstrumentTypeAsync(ctx, "Guitar");
        var store = await SeedStoreAsync(ctx, "Music Shop Sarajevo");
        await SeedStoreAsync(ctx, "Music Shop Mostar");
        var service = CreateStoreInstrumentService(ctx, store.Id, new StubFileStorageService());

        var dto = await service.CreateAsync(new InstrumentRequest
        {
            Model = "  Stratocaster  ",
            ManufacturerId = TestManufacturers.Id("Fender"),
            Description = "  First guitar  ",
            InstrumentTypeId = type.Id
        });

        Assert.Equal("Music Shop Sarajevo", dto.MusicStore);
        Assert.Equal("Stratocaster", dto.Model);
        Assert.Equal("Fender", dto.ManufacturerName);
        Assert.Equal("First guitar", dto.Description);

        var stored = await ctx.Set<Instrument>().AsNoTracking().SingleAsync(x => x.Id == dto.Id);
        Assert.Equal(store.Id, stored.MusicStoreId);
        Assert.Equal("Stratocaster", stored.Model);
        Assert.Equal(TestManufacturers.Id("Fender"), stored.ManufacturerId);
        Assert.Equal("First guitar", stored.Description);
    }

    [Fact]
    public async Task CreateAsync_UnknownManufacturer_ThrowsBusinessException()
    {
        var ctx = CreateContext();
        var type = await SeedInstrumentTypeAsync(ctx, "Guitar");
        var store = await SeedStoreAsync(ctx, "Music Shop Sarajevo");
        var service = CreateStoreInstrumentService(ctx, store.Id, new StubFileStorageService());

        var ex = await Assert.ThrowsAsync<BusinessException>(() => service.CreateAsync(new InstrumentRequest
        {
            Model = "Stratocaster",
            ManufacturerId = 9999,
            InstrumentTypeId = type.Id
        }));

        Assert.Equal(Messages.ManufacturerNotFound, ex.Message);
    }

    [Fact]
    public async Task CreateAsync_UnknownInstrumentType_ThrowsBusinessException()
    {
        var ctx = CreateContext();
        var type = await SeedInstrumentTypeAsync(ctx, "Guitar");
        var store = await SeedStoreAsync(ctx, "Music Shop Sarajevo");
        var service = CreateStoreInstrumentService(ctx, store.Id, new StubFileStorageService());

        var ex = await Assert.ThrowsAsync<BusinessException>(() => service.CreateAsync(new InstrumentRequest
        {
            Model = "Stratocaster",
            ManufacturerId = TestManufacturers.Id("Fender"),
            InstrumentTypeId = type.Id + 1000
        }));

        Assert.Equal(Messages.InstrumentTypeNotFound, ex.Message);
    }

    [Fact]
    public async Task UpdateAsync_ArchivedInstrument_ThrowsNotFoundException()
    {
        var ctx = CreateContext();
        var type = await SeedInstrumentTypeAsync(ctx, "Guitar");
        var store = await SeedStoreAsync(ctx, "Music Shop Sarajevo");
        var instrument = await SeedInstrumentAsync(ctx, store.Id, type.Id, "Stratocaster", "Fender");
        instrument.SoftDelete();
        await ctx.SaveChangesAsync();
        var service = CreateStoreInstrumentService(ctx, store.Id, new StubFileStorageService());

        await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(instrument.Id, new InstrumentRequest
        {
            Model = "Les Paul",
            ManufacturerId = TestManufacturers.Id("Gibson"),
            InstrumentTypeId = type.Id
        }));
    }

    [Fact]
    public async Task UploadImageAsync_ArchivedInstrument_ThrowsNotFoundException()
    {
        var ctx = CreateContext();
        var type = await SeedInstrumentTypeAsync(ctx, "Guitar");
        var store = await SeedStoreAsync(ctx, "Music Shop Sarajevo");
        var instrument = await SeedInstrumentAsync(ctx, store.Id, type.Id, "Stratocaster", "Fender");
        instrument.SoftDelete();
        await ctx.SaveChangesAsync();
        var service = CreateStoreInstrumentService(ctx, store.Id, new RecordingFileStorageService());

        using var stream = new MemoryStream([1, 2, 3]);
        await Assert.ThrowsAsync<NotFoundException>(() => service.UploadImageAsync(instrument.Id, stream, "image/png"));
    }

    [Fact]
    public async Task UploadImageAsync_DeletesNewFile_WhenSaveFails()
    {
        var ctx = CreateContext();
        var type = await SeedInstrumentTypeAsync(ctx, "Guitar");
        var store = await SeedStoreAsync(ctx, "Music Shop Sarajevo");
        var instrument = await SeedInstrumentAsync(ctx, store.Id, type.Id, "Stratocaster", "Fender");
        var storage = new RecordingFileStorageService();
        var service = CreateStoreInstrumentService(new ThrowingSaveDbContext(ctx, new InvalidOperationException("save failed")), store.Id, storage);

        using var stream = new MemoryStream([1, 2, 3]);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UploadImageAsync(instrument.Id, stream, "image/png"));

        Assert.Equal(storage.SavedPaths, storage.DeletedPaths);
    }

    [Fact]
    public async Task UploadImageAsync_DeletesPreviousImage_AfterReplacement()
    {
        var ctx = CreateContext();
        var type = await SeedInstrumentTypeAsync(ctx, "Guitar");
        var store = await SeedStoreAsync(ctx, "Music Shop Sarajevo");
        var instrument = new Instrument("Stratocaster", TestManufacturers.Id("Fender"), null, "/old/instrument.png", type.Id, store.Id);
        ctx.Set<Instrument>().Add(instrument);
        await ctx.SaveChangesAsync();
        var storage = new RecordingFileStorageService();
        var service = CreateStoreInstrumentService(ctx, store.Id, storage);

        using var stream = new MemoryStream([1, 2, 3]);
        var updated = await service.UploadImageAsync(instrument.Id, stream, "image/png");

        Assert.Equal(storage.SavedPaths.Single(), updated.ImagePath);
        Assert.Equal(["/old/instrument.png"], storage.DeletedPaths);
    }

    [Fact]
    public async Task GetPagedAsync_ProjectsIsAvailable_FalseWhenApprovedOrActiveRentalExists()
    {
        var ctx = CreateContext();
        var type = await SeedInstrumentTypeAsync(ctx, "Guitar");
        var store = await SeedStoreAsync(ctx, "Music Shop Sarajevo");
        var instrumentAvailable = await SeedInstrumentAsync(ctx, store.Id, type.Id, "Pacifica", "Yamaha");
        var instrumentRented = await SeedInstrumentAsync(ctx, store.Id, type.Id, "Stratocaster", "Fender");

        var blockingRental = new InstrumentRental(instrumentRented.Id, 100, store.Id, Now, null);
        RentalTestData.AdvanceTo(blockingRental, InstrumentRentalStatus.Approved, Now);
        ctx.Set<InstrumentRental>().Add(blockingRental);
        await ctx.SaveChangesAsync();

        var service = CreateInstrumentService(ctx);

        var paged = await service.GetPagedAsync(new InstrumentSearchObject());

        Assert.Equal(2, paged.Items.Count);
        var availDto = paged.Items.Single(x => x.Id == instrumentAvailable.Id);
        var rentedDto = paged.Items.Single(x => x.Id == instrumentRented.Id);

        Assert.True(availDto.IsAvailable);
        Assert.False(rentedDto.IsAvailable);
    }

    [Fact]
    public async Task GetPagedAsync_IsAvailable_CountsOnlyApprovedAndActiveAsUnavailable()
    {
        var ctx = CreateContext();
        var type = await SeedInstrumentTypeAsync(ctx, "Guitar");
        var store = await SeedStoreAsync(ctx, "Music Shop Sarajevo");
        var statuses = new[]
        {
            InstrumentRentalStatus.Pending,
            InstrumentRentalStatus.Rejected,
            InstrumentRentalStatus.Canceled,
            InstrumentRentalStatus.Completed,
            InstrumentRentalStatus.Approved,
            InstrumentRentalStatus.Active
        };

        var idsByStatus = new Dictionary<InstrumentRentalStatus, int>();

        foreach (var status in statuses)
        {
            var instrument = await SeedInstrumentAsync(ctx, store.Id, type.Id, $"{status} model", "Maker");
            idsByStatus[status] = instrument.Id;

            var rental = new InstrumentRental(instrument.Id, 100, store.Id, Now, null);
            RentalTestData.AdvanceTo(rental, status, Now, approvedAt: Now.AddDays(-31), pickedUpAt: Now.AddDays(-31), finishedAt: Now);
            ctx.Set<InstrumentRental>().Add(rental);
        }

        await ctx.SaveChangesAsync();
        var service = CreateInstrumentService(ctx);

        var available = await service.GetPagedAsync(new InstrumentSearchObject { IsAvailable = true });
        var unavailable = await service.GetPagedAsync(new InstrumentSearchObject { IsAvailable = false });

        Assert.Equal(
            new[] { InstrumentRentalStatus.Pending, InstrumentRentalStatus.Rejected, InstrumentRentalStatus.Canceled, InstrumentRentalStatus.Completed }
                .Select(status => idsByStatus[status])
                .Order(),
            available.Items.Select(x => x.Id).Order());
        Assert.Equal(
            new[] { InstrumentRentalStatus.Approved, InstrumentRentalStatus.Active }
                .Select(status => idsByStatus[status])
                .Order(),
            unavailable.Items.Select(x => x.Id).Order());
        Assert.All(available.Items, x => Assert.True(x.IsAvailable));
        Assert.All(unavailable.Items, x => Assert.False(x.IsAvailable));
    }

    [Fact]
    public async Task GetPagedAsync_ExcludesArchivedInstruments()
    {
        var ctx = CreateContext();
        var type = await SeedInstrumentTypeAsync(ctx, "Guitar");
        var store = await SeedStoreAsync(ctx, "Music Shop Sarajevo");
        var active = await SeedInstrumentAsync(ctx, store.Id, type.Id, "Stratocaster", "Fender");
        var archived = await SeedInstrumentAsync(ctx, store.Id, type.Id, "Les Paul", "Gibson");
        archived.SoftDelete();
        await ctx.SaveChangesAsync();

        var service = CreateInstrumentService(ctx);

        var paged = await service.GetPagedAsync(new InstrumentSearchObject());

        Assert.Equal([active.Id], paged.Items.Select(x => x.Id));
        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(archived.Id));
    }

    [Fact]
    public async Task GetByIdAsync_ProjectsIsAvailable_AccuratelyInSql()
    {
        var ctx = CreateContext();
        var type = await SeedInstrumentTypeAsync(ctx, "Guitar");
        var store = await SeedStoreAsync(ctx, "Music Shop Sarajevo");
        var instrument = await SeedInstrumentAsync(ctx, store.Id, type.Id, "Stratocaster", "Fender");

        var rental = new InstrumentRental(instrument.Id, 100, store.Id, Now, null);
        RentalTestData.AdvanceTo(rental, InstrumentRentalStatus.Approved, Now);
        ctx.Set<InstrumentRental>().Add(rental);
        await ctx.SaveChangesAsync();

        var service = CreateInstrumentService(ctx);

        var dto = await service.GetByIdAsync(instrument.Id);

        Assert.False(dto.IsAvailable);
        Assert.Equal("Stratocaster", dto.Model);
        Assert.Equal("Music Shop Sarajevo", dto.MusicStore);
        Assert.Equal("Guitar", dto.InstrumentType);
    }

    [Fact]
    public async Task WithInstrumentDetails_DoesNotEagerlyLoadInstrumentRentals()
    {
        var ctx = CreateContext();
        var type = await SeedInstrumentTypeAsync(ctx, "Guitar");
        var store = await SeedStoreAsync(ctx, "Music Shop Sarajevo");
        var instrument = await SeedInstrumentAsync(ctx, store.Id, type.Id, "Stratocaster", "Fender");

        var rental = new InstrumentRental(instrument.Id, 100, store.Id, Now, null);
        RentalTestData.AdvanceTo(rental, InstrumentRentalStatus.Approved, Now);
        ctx.Set<InstrumentRental>().Add(rental);
        await ctx.SaveChangesAsync();

        var loaded = await ctx.Set<Instrument>()
            .AsNoTracking()
            .WithInstrumentDetails()
            .FirstAsync(x => x.Id == instrument.Id);

        Assert.NotNull(loaded.InstrumentType);
        Assert.NotNull(loaded.MusicStore);
        Assert.Empty(loaded.InstrumentRentals);
    }

    [Theory]
    [InlineData(InstrumentRentalStatus.Approved)]
    [InlineData(InstrumentRentalStatus.Active)]
    [InlineData(InstrumentRentalStatus.Pending)]
    [InlineData(InstrumentRentalStatus.Rejected)]
    [InlineData(InstrumentRentalStatus.Canceled)]
    [InlineData(InstrumentRentalStatus.Completed)]
    [InlineData(InstrumentRentalStatus.ReturnedEarly)]
    public async Task DeleteAsync_AppliesArchiveRulesForEveryRentalStatus(InstrumentRentalStatus status)
    {
        var ctx = CreateContext();
        var type = await SeedInstrumentTypeAsync(ctx, "Guitar");
        var store = await SeedStoreAsync(ctx, "Music Shop Sarajevo");
        var instrument = await SeedInstrumentAsync(ctx, store.Id, type.Id, "Stratocaster", "Fender");
        var student = new Student(appUserId: 100, enrollmentDate: Now.AddMonths(-1));
        ctx.Set<Student>().Add(student);
        await ctx.SaveChangesAsync();

        var rental = await SeedRentalInStatusAsync(ctx, instrument, student, status);
        var noteBeforeDelete = rental.Note;
        var recorder = new RecordingNotificationDispatcher();
        var service = CreateStoreInstrumentService(ctx, store.Id, new StubFileStorageService(), recorder);

        if (status is InstrumentRentalStatus.Approved or InstrumentRentalStatus.Active)
        {
            var ex = await Assert.ThrowsAsync<BusinessException>(() => service.DeleteAsync(instrument.Id));

            Assert.Equal(Messages.InstrumentDeleteBlocked, ex.Message);
            Assert.True((await ctx.Set<Instrument>().IgnoreQueryFilters().SingleAsync(x => x.Id == instrument.Id)).IsActive);
            Assert.Equal(status, (await ctx.Set<InstrumentRental>().AsNoTracking().SingleAsync(x => x.Id == rental.Id)).RentalStatus);
            Assert.Empty(recorder.TransitionCalls);
            return;
        }

        await service.DeleteAsync(instrument.Id);

        Assert.False((await ctx.Set<Instrument>().IgnoreQueryFilters().SingleAsync(x => x.Id == instrument.Id)).IsActive);
        var stored = await ctx.Set<InstrumentRental>().AsNoTracking().SingleAsync(x => x.Id == rental.Id);

        if (status == InstrumentRentalStatus.Pending)
        {
            Assert.Equal(InstrumentRentalStatus.Rejected, stored.RentalStatus);
            Assert.Equal(Messages.RentalRejectedInstrumentArchived, stored.DecisionNote);
            Assert.Null(stored.Note);
            Assert.Equal(Now, stored.RejectedAt);

            var notification = Assert.Single(recorder.TransitionCalls);
            Assert.Equal(RentalTrigger.Reject, notification.Trigger);
            Assert.Equal(rental.Id, notification.Rental.Id);
            Assert.Equal(student.AppUserId, notification.Rental.StudentUserId);
            Assert.Equal(InstrumentRentalStatus.Rejected, notification.Rental.RentalStatus);
            return;
        }

        Assert.Equal(status, stored.RentalStatus);
        Assert.Equal(noteBeforeDelete, stored.Note);
        Assert.Empty(recorder.TransitionCalls);
    }

    [Fact]
    public async Task DeleteAsync_KeepsStoredImage_BecauseRentalHistoryUsesIt()
    {
        var ctx = CreateContext();
        var type = await SeedInstrumentTypeAsync(ctx, "Guitar");
        var store = await SeedStoreAsync(ctx, "Music Shop Sarajevo");
        var instrument = new Instrument("Stratocaster", TestManufacturers.Id("Fender"), null, "/api/v1/uploads/instruments/old.png", type.Id, store.Id);
        ctx.Set<Instrument>().Add(instrument);
        await ctx.SaveChangesAsync();
        var student = new Student(appUserId: 100, enrollmentDate: Now.AddMonths(-1));
        ctx.Set<Student>().Add(student);
        await ctx.SaveChangesAsync();
        var rental = RentalTestData.CreateCompletedRental(instrument, student.Id, Now);
        ctx.Set<InstrumentRental>().Add(rental);
        await ctx.SaveChangesAsync();
        var storage = new RecordingFileStorageService();
        var service = CreateStoreInstrumentService(ctx, store.Id, storage);

        await service.DeleteAsync(instrument.Id);

        Assert.False((await ctx.Set<Instrument>().IgnoreQueryFilters().SingleAsync(x => x.Id == instrument.Id)).IsActive);
        Assert.Empty(storage.DeletedPaths);

        var queryService = new RentalQueryService(
            ctx,
            TestMapper.Create(),
            new StubCurrentActor(student: student, storeId: store.Id),
            new FixedClock(Now),
            new StubDisplayNameService());
        var dto = await queryService.GetByIdForStudentAsync(rental.Id);

        Assert.Equal("/api/v1/uploads/instruments/old.png", dto.InstrumentImagePath);
    }

    [Fact]
    public async Task UploadImageAsync_ThrowsQuotaExceeded_WhenStoreUsageWouldExceed()
    {
        var ctx = CreateContext();
        var type = await SeedInstrumentTypeAsync(ctx, "Guitar");
        var store = await SeedStoreAsync(ctx, "Music Shop Sarajevo");
        var existing = new Instrument("Pacifica", TestManufacturers.Id("Yamaha"), null, "/api/v1/uploads/instruments/existing.png", type.Id, store.Id);
        var target = new Instrument("Stratocaster", TestManufacturers.Id("Fender"), null, null, type.Id, store.Id);
        ctx.Set<Instrument>().AddRange(existing, target);
        await ctx.SaveChangesAsync();
        var storage = new RecordingFileStorageService();
        storage.FileSizes["/api/v1/uploads/instruments/existing.png"] = FileUploadLimits.MaxStoreUploadBytes;
        var service = CreateStoreInstrumentService(ctx, store.Id, storage);

        using var stream = new MemoryStream([1, 2, 3]);
        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            service.UploadImageAsync(target.Id, stream, "image/png"));

        Assert.Equal(Messages.StorageQuotaExceeded, ex.Message);
        Assert.Empty(storage.SavedPaths);
    }

    [Fact]
    public async Task UploadImageAsync_AllowsReplacement_WhenPreviousImageFillsQuota()
    {
        var ctx = CreateContext();
        var type = await SeedInstrumentTypeAsync(ctx, "Guitar");
        var store = await SeedStoreAsync(ctx, "Music Shop Sarajevo");
        var instrument = new Instrument("Stratocaster", TestManufacturers.Id("Fender"), null, "/api/v1/uploads/instruments/existing.png", type.Id, store.Id);
        ctx.Set<Instrument>().Add(instrument);
        await ctx.SaveChangesAsync();
        var storage = new RecordingFileStorageService();
        storage.FileSizes["/api/v1/uploads/instruments/existing.png"] = FileUploadLimits.MaxStoreUploadBytes;
        var service = CreateStoreInstrumentService(ctx, store.Id, storage);

        using var stream = new MemoryStream([1, 2, 3]);
        var updated = await service.UploadImageAsync(instrument.Id, stream, "image/png");

        Assert.Equal(storage.SavedPaths.Single(), updated.ImagePath);
    }

    [Fact]
    public async Task UploadImageAsync_DoesNotCountArchivedInstrumentImages()
    {
        var ctx = CreateContext();
        var type = await SeedInstrumentTypeAsync(ctx, "Guitar");
        var store = await SeedStoreAsync(ctx, "Music Shop Sarajevo");
        var archived = new Instrument("Pacifica", TestManufacturers.Id("Yamaha"), null, "/api/v1/uploads/instruments/archived.png", type.Id, store.Id);
        var target = new Instrument("Stratocaster", TestManufacturers.Id("Fender"), null, null, type.Id, store.Id);
        ctx.Set<Instrument>().AddRange(archived, target);
        await ctx.SaveChangesAsync();
        archived.SoftDelete();
        await ctx.SaveChangesAsync();
        var storage = new RecordingFileStorageService();
        storage.FileSizes["/api/v1/uploads/instruments/archived.png"] = 524_288_000;
        var service = CreateStoreInstrumentService(ctx, store.Id, storage);

        using var stream = new MemoryStream([1, 2, 3]);
        var updated = await service.UploadImageAsync(target.Id, stream, "image/png");

        Assert.Equal(storage.SavedPaths.Single(), updated.ImagePath);
    }

    [Theory]
    [InlineData(524_287_997, true)]
    [InlineData(524_287_998, false)]
    public async Task UploadImageAsync_AppliesTheQuotaAtTheExactBoundary(long usedBytes, bool accepted)
    {
        var ctx = CreateContext();
        var type = await SeedInstrumentTypeAsync(ctx, "Guitar");
        var store = await SeedStoreAsync(ctx, "Music Shop Sarajevo");
        var existing = new Instrument("Pacifica", TestManufacturers.Id("Yamaha"), null, "/api/v1/uploads/instruments/existing.png", type.Id, store.Id);
        var target = new Instrument("Stratocaster", TestManufacturers.Id("Fender"), null, null, type.Id, store.Id);
        ctx.Set<Instrument>().AddRange(existing, target);
        await ctx.SaveChangesAsync();
        var storage = new RecordingFileStorageService();
        storage.FileSizes["/api/v1/uploads/instruments/existing.png"] = usedBytes;
        var service = CreateStoreInstrumentService(ctx, store.Id, storage);

        using var stream = new MemoryStream([1, 2, 3]);

        if (accepted)
        {
            await service.UploadImageAsync(target.Id, stream, "image/png");
            Assert.Single(storage.SavedPaths);
            return;
        }

        var exception = await Assert.ThrowsAsync<BusinessException>(() => service.UploadImageAsync(target.Id, stream, "image/png"));
        Assert.Equal(Messages.StorageQuotaExceeded, exception.Message);
        Assert.Empty(storage.SavedPaths);
    }

    [Fact]
    public async Task UploadImageAsync_DoesNotCountAnotherStoresImages()
    {
        var ctx = CreateContext();
        var type = await SeedInstrumentTypeAsync(ctx, "Guitar");
        var store = await SeedStoreAsync(ctx, "Music Shop Sarajevo");
        var otherStore = await SeedStoreAsync(ctx, "Music Shop Mostar");
        var foreign = new Instrument("Pacifica", TestManufacturers.Id("Yamaha"), null, "/api/v1/uploads/instruments/foreign.png", type.Id, otherStore.Id);
        var target = new Instrument("Stratocaster", TestManufacturers.Id("Fender"), null, null, type.Id, store.Id);
        ctx.Set<Instrument>().AddRange(foreign, target);
        await ctx.SaveChangesAsync();
        var storage = new RecordingFileStorageService();
        storage.FileSizes["/api/v1/uploads/instruments/foreign.png"] = 524_288_000;
        var service = CreateStoreInstrumentService(ctx, store.Id, storage);

        using var stream = new MemoryStream([1, 2, 3]);
        await service.UploadImageAsync(target.Id, stream, "image/png");

        Assert.Single(storage.SavedPaths);
    }

    [Fact]
    public async Task UpdateAsync_NullDescription_ClearsStoredDescription()
    {
        var ctx = CreateContext();
        var type = await SeedInstrumentTypeAsync(ctx, "Guitar");
        var store = await SeedStoreAsync(ctx, "Music Shop Sarajevo");
        var instrument = new Instrument("Stratocaster", TestManufacturers.Id("Fender"), "Old text", null, type.Id, store.Id);
        ctx.Set<Instrument>().Add(instrument);
        await ctx.SaveChangesAsync();
        var service = CreateStoreInstrumentService(ctx, store.Id, new StubFileStorageService());

        var updated = await service.UpdateAsync(instrument.Id, new InstrumentRequest
        {
            Model = "Stratocaster",
            ManufacturerId = TestManufacturers.Id("Fender"),
            Description = null,
            InstrumentTypeId = type.Id
        });

        Assert.Null(updated.Description);
        var stored = await ctx.Set<Instrument>().AsNoTracking().SingleAsync(x => x.Id == instrument.Id);
        Assert.Null(stored.Description);
    }

    [Fact]
    public async Task UpdateAsync_BlankDescription_StoresNull()
    {
        var ctx = CreateContext();
        var type = await SeedInstrumentTypeAsync(ctx, "Guitar");
        var store = await SeedStoreAsync(ctx, "Music Shop Sarajevo");
        var instrument = new Instrument("Stratocaster", TestManufacturers.Id("Fender"), "Old text", null, type.Id, store.Id);
        ctx.Set<Instrument>().Add(instrument);
        await ctx.SaveChangesAsync();
        var service = CreateStoreInstrumentService(ctx, store.Id, new StubFileStorageService());

        var updated = await service.UpdateAsync(instrument.Id, new InstrumentRequest
        {
            Model = "Stratocaster",
            ManufacturerId = TestManufacturers.Id("Fender"),
            Description = "   ",
            InstrumentTypeId = type.Id
        });

        Assert.Null(updated.Description);
        var stored = await ctx.Set<Instrument>().AsNoTracking().SingleAsync(x => x.Id == instrument.Id);
        Assert.Null(stored.Description);
    }

    [Fact]
    public async Task UpdateAsync_TrimsModelAndChangesManufacturer()
    {
        var ctx = CreateContext();
        var type = await SeedInstrumentTypeAsync(ctx, "Guitar");
        var store = await SeedStoreAsync(ctx, "Music Shop Sarajevo");
        var instrument = await SeedInstrumentAsync(ctx, store.Id, type.Id, "Stratocaster", "Fender");
        var service = CreateStoreInstrumentService(ctx, store.Id, new StubFileStorageService());

        var updated = await service.UpdateAsync(instrument.Id, new InstrumentRequest
        {
            Model = "  Les Paul  ",
            ManufacturerId = TestManufacturers.Id("Gibson"),
            InstrumentTypeId = type.Id
        });

        Assert.Equal("Les Paul", updated.Model);
        Assert.Equal("Gibson", updated.ManufacturerName);
        var stored = await ctx.Set<Instrument>().AsNoTracking().SingleAsync(x => x.Id == instrument.Id);
        Assert.Equal("Les Paul", stored.Model);
        Assert.Equal(TestManufacturers.Id("Gibson"), stored.ManufacturerId);
    }

    [Fact]
    public async Task UpdateAsync_UnknownInstrumentType_ThrowsBusinessException()
    {
        var ctx = CreateContext();
        var type = await SeedInstrumentTypeAsync(ctx, "Guitar");
        var store = await SeedStoreAsync(ctx, "Music Shop Sarajevo");
        var instrument = await SeedInstrumentAsync(ctx, store.Id, type.Id, "Stratocaster", "Fender");
        var service = CreateStoreInstrumentService(ctx, store.Id, new StubFileStorageService());

        var ex = await Assert.ThrowsAsync<BusinessException>(() => service.UpdateAsync(instrument.Id, new InstrumentRequest
        {
            Model = "Stratocaster",
            ManufacturerId = TestManufacturers.Id("Fender"),
            InstrumentTypeId = type.Id + 1000
        }));

        Assert.Equal(Messages.InstrumentTypeNotFound, ex.Message);
    }

    private static ENoteContext CreateContext()
    {
        var context = new ENoteContext(TestDbContextFactory.CreateOptions(), new FixedClock(Now), new StubCurrentActor());
        TestManufacturers.Seed(context);

        return context;
    }

    private static InstrumentService CreateInstrumentService(ENoteContext context)
    {
        var actor = new StubCurrentActor();
        return new(context, actor, new StubFileStorageService(), CreateRentalCommandService(context, actor));
    }

    private static InstrumentService CreateStoreInstrumentService(IAppDbContext context, int storeId, IFileStorageService storage, IRentalNotificationDispatcher? dispatcher = null)
    {
        var actor = new StubCurrentActor(storeId: storeId, employee: new MusicStoreEmployee(appUserId: 1, musicStoreId: storeId, isManager: true));
        return new(context, actor, storage, CreateRentalCommandService(context, actor, dispatcher));
    }

    private static RentalCommandService CreateRentalCommandService(IAppDbContext context, StubCurrentActor actor, IRentalNotificationDispatcher? dispatcher = null) =>
        new(context, TestMapper.Create(), new FixedClock(Now), actor, actor, actor, dispatcher ?? new RecordingNotificationDispatcher(), new StubDisplayNameService(), new RentalOptions { OverduePenaltyMultiplier = 0.5m });

    private static async Task<MusicStore> SeedStoreAsync(ENoteContext ctx, string name)
    {
        var store = new MusicStore(name, "09-17");
        ctx.Set<MusicStore>().Add(store);
        await ctx.SaveChangesAsync();
        return store;
    }

    private static async Task<InstrumentType> SeedInstrumentTypeAsync(ENoteContext ctx, string typeName, decimal monthlyFee = 50m)
    {
        var type = new InstrumentType { Type = typeName, MonthlyFee = monthlyFee };
        ctx.Set<InstrumentType>().Add(type);
        await ctx.SaveChangesAsync();
        return type;
    }

    private static async Task<Instrument> SeedInstrumentAsync(ENoteContext ctx, int storeId, int typeId, string model, string manufacturer)
    {
        var instrument = new Instrument(model, TestManufacturers.Id(manufacturer), null, null, typeId, storeId);
        ctx.Set<Instrument>().Add(instrument);
        await ctx.SaveChangesAsync();
        return instrument;
    }

    private static async Task<InstrumentRental> SeedRentalInStatusAsync(
        ENoteContext ctx,
        Instrument instrument,
        Student student,
        InstrumentRentalStatus status)
    {
        var rental = new InstrumentRental(instrument.Id, student.Id, instrument.MusicStoreId, Now, "Original request note.");
        var completed = status == InstrumentRentalStatus.Completed;

        RentalTestData.AdvanceTo(
            rental,
            status,
            Now,
            approvedAt: completed ? Now.AddDays(-30) : null,
            pickedUpAt: completed ? Now.AddDays(-30) : null,
            finishedAt: completed ? Now : null);

        ctx.Set<InstrumentRental>().Add(rental);
        await ctx.SaveChangesAsync();
        return rental;
    }
}
