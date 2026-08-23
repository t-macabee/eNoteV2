using eNote.Application.Features.Communication.Announcements;
using eNote.Application.Features.Communication.Announcements.Services;
using eNote.Application.Features.Identity.Instructors;
using eNote.Domain.Entities.Communication;
using eNote.Domain.Entities.Rentals;
using eNote.Tests.TestUtils;

namespace eNote.Tests.Communication;

public sealed class StudentAnnouncementFeedServiceTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetFeedForStudentAsync_IncludesCourseAndStoreAnnouncements()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var store = new MusicStore("Music Shop", "09-17");
        harness.Context.Set<MusicStore>().Add(store);
        await harness.Context.SaveChangesAsync();
        var type = new InstrumentType { Type = "Guitar", MonthlyFee = 50m };
        harness.Context.Set<InstrumentType>().Add(type);
        await harness.Context.SaveChangesAsync();
        var instrument = new Instrument("Strat", "Fender", null, null, type.Id, store.Id);
        harness.Context.Set<Instrument>().Add(instrument);
        await harness.Context.SaveChangesAsync();
        var rental = new InstrumentRental(instrument.Id, harness.Student.Id, store.Id, Now, null);
        rental.Approve(50m, null, Now, 1);
        harness.Context.Set<InstrumentRental>().Add(rental);
        await harness.Context.SaveChangesAsync();

        harness.Context.Set<Announcement>().AddRange(
            new Announcement("Course note", "For the course", harness.Course.Id, null, Now),
            new Announcement("Store note", "For the store", null, store.Id, Now),
            new Announcement("Other course", "Not enrolled", 999, null, Now));
        await harness.Context.SaveChangesAsync();

        var actor = new StubCurrentActor(student: harness.Student);
        var service = new StudentAnnouncementFeedService(harness.Context, actor, TestMapper.Create());

        var result = await service.GetFeedForStudentAsync(new AnnouncementSearchObject { Page = 1, PageSize = 10 });

        Assert.Equal(2, result.Items.Count);
        Assert.Contains(result.Items, a => a.Title == "Course note");
        Assert.Contains(result.Items, a => a.Title == "Store note");
    }
}
