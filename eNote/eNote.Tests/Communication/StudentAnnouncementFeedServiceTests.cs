using Microsoft.EntityFrameworkCore;

using eNote.Application.Common.Localization;
using eNote.Application.Features.Communication.Announcements;
using eNote.Application.Features.Communication.Announcements.Services;
using eNote.Domain.Entities.Communication;
using eNote.Tests.TestUtils;

namespace eNote.Tests.Communication;

public sealed class StudentAnnouncementFeedServiceTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetFeedForStudentAsync_IncludesCourseAndStoreAnnouncements()
    {
        var harness = await AcademicTestData.SeedAsync(CreateStudentContext(), Now);
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
        RentalTestData.AdvanceTo(rental, InstrumentRentalStatus.Approved, Now);
        harness.Context.Set<InstrumentRental>().Add(rental);
        await harness.Context.SaveChangesAsync();

        harness.Context.Set<Announcement>().AddRange(
            new Announcement("Course note", "For the course", harness.Course.Id, null, Now),
            new Announcement("Store note", "For the store", null, store.Id, Now),
            new Announcement("Other course", "Not enrolled", 999, null, Now));
        await harness.Context.SaveChangesAsync();

        var actor = new StubCurrentActor(student: harness.Student);
        var service = new StudentAnnouncementFeedService(harness.Context, actor, TestMapper.Create(), new FixedClock(Now));

        var result = await service.GetFeedForStudentAsync(new AnnouncementSearchObject { Page = 1, PageSize = 10 });

        Assert.Equal(2, result.Items.Count);
        Assert.Contains(result.Items, a => a.Title == "Course note");
        Assert.Contains(result.Items, a => a.Title == "Store note");
    }

    [Fact]
    public async Task GetFeedForStudentAsync_ExcludesCoursesAndStores_WithoutActiveEnrollmentOrRental()
    {
        var harness = await AcademicTestData.SeedAsync(CreateStudentContext(), Now);
        var secondStudent = await AcademicTestData.AddSecondInstructorContentAsync(harness, Now);

        var type = new InstrumentType { Type = "Guitar", MonthlyFee = 50m };
        harness.Context.Set<InstrumentType>().Add(type);
        await harness.Context.SaveChangesAsync();

        var storeB = new MusicStore("Store B", "09-17");
        harness.Context.Set<MusicStore>().Add(storeB);
        await harness.Context.SaveChangesAsync();
        var instrumentB = new Instrument("Strat B", "Fender", null, null, type.Id, storeB.Id);
        harness.Context.Set<Instrument>().Add(instrumentB);
        await harness.Context.SaveChangesAsync();
        var completedRental = new InstrumentRental(instrumentB.Id, secondStudent.Student.Id, storeB.Id, Now, null);
        RentalTestData.AdvanceTo(completedRental, InstrumentRentalStatus.Completed, Now);
        harness.Context.Set<InstrumentRental>().Add(completedRental);
        await harness.Context.SaveChangesAsync();

        var pendingCourse = new Course("Cello pending", null, 100m, Now, Now.AddMonths(6), harness.Instructor.Id);
        var rejectedCourse = new Course("Cello rejected", null, 100m, Now, Now.AddMonths(6), harness.Instructor.Id);
        harness.Context.Set<Course>().AddRange(pendingCourse, rejectedCourse);
        await harness.Context.SaveChangesAsync();
        harness.Context.Set<Enrollment>().AddRange(
            new Enrollment(harness.Student.Id, pendingCourse.Id, EnrollmentStatus.Pending),
            new Enrollment(harness.Student.Id, rejectedCourse.Id, EnrollmentStatus.Rejected));
        await harness.Context.SaveChangesAsync();

        var storeD = new MusicStore("Store D", "09-17");
        harness.Context.Set<MusicStore>().Add(storeD);
        await harness.Context.SaveChangesAsync();
        var instrumentD = new Instrument("Strat D", "Fender", null, null, type.Id, storeD.Id);
        harness.Context.Set<Instrument>().Add(instrumentD);
        await harness.Context.SaveChangesAsync();
        var pendingRental = new InstrumentRental(instrumentD.Id, harness.Student.Id, storeD.Id, Now, null);
        var rejectedRental = new InstrumentRental(instrumentD.Id, harness.Student.Id, storeD.Id, Now, null);
        RentalTestData.AdvanceTo(rejectedRental, InstrumentRentalStatus.Rejected, Now);
        harness.Context.Set<InstrumentRental>().AddRange(pendingRental, rejectedRental);
        await harness.Context.SaveChangesAsync();

        harness.Context.Set<Announcement>().AddRange(
            new Announcement("Course A note", "For the course", harness.Course.Id, null, Now),
            new Announcement("Course B note", "For the other course", secondStudent.Course.Id, null, Now),
            new Announcement("Store B note", "For the other store", null, storeB.Id, Now),
            new Announcement("Pending course note", "Not active", pendingCourse.Id, null, Now),
            new Announcement("Rejected course note", "Not active", rejectedCourse.Id, null, Now),
            new Announcement("Store D note", "Not an active rental", null, storeD.Id, Now));
        await harness.Context.SaveChangesAsync();

        var actor = new StubCurrentActor(student: harness.Student);
        var service = new StudentAnnouncementFeedService(harness.Context, actor, TestMapper.Create(), new FixedClock(Now));

        var result = await service.GetFeedForStudentAsync(new AnnouncementSearchObject { Page = 1, PageSize = 20 });

        Assert.Equal(["Course A note"], result.Items.Select(a => a.Title));
    }

    [Fact]
    public async Task GetFeedForStudentAsync_IncludesStoreAnnouncements_WhenInstrumentSoftDeleted()
    {
        var harness = await AcademicTestData.SeedAsync(CreateStudentContext(), Now);
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
        RentalTestData.AdvanceTo(rental, InstrumentRentalStatus.Completed, Now);
        harness.Context.Set<InstrumentRental>().Add(rental);
        await harness.Context.SaveChangesAsync();

        instrument.SoftDelete();
        await harness.Context.SaveChangesAsync();

        harness.Context.Set<Announcement>().Add(new Announcement("Store note", "For the store", null, store.Id, Now));
        await harness.Context.SaveChangesAsync();

        var actor = new StubCurrentActor(student: harness.Student);
        var service = new StudentAnnouncementFeedService(harness.Context, actor, TestMapper.Create(), new FixedClock(Now));

        var result = await service.GetFeedForStudentAsync(new AnnouncementSearchObject { Page = 1, PageSize = 10 });

        Assert.Contains(result.Items, a => a.Title == "Store note");
    }

    [Fact]
    public async Task GetByIdForStudentAsync_ReturnsCourseAnnouncement_WhenEnrollmentActive()
    {
        var harness = await AcademicTestData.SeedAsync(CreateStudentContext(), Now);
        harness.Context.Set<Announcement>().Add(new Announcement("Course note", "For the course", harness.Course.Id, null, Now) { Id = 101 });
        await harness.Context.SaveChangesAsync();

        var result = await CreateService(harness).GetByIdForStudentAsync(101);

        Assert.Equal(101, result.Id);
        Assert.Equal("Course note", result.Title);
        Assert.Equal(harness.Course.Name, result.CourseName);
    }

    [Fact]
    public async Task GetByIdForStudentAsync_ReturnsStoreAnnouncement_WhenRentalCompleted()
    {
        var harness = await AcademicTestData.SeedAsync(CreateStudentContext(), Now);
        var store = await AddStoreWithRentalAsync(harness, harness.Student.Id, InstrumentRentalStatus.Completed, "Music Shop");
        harness.Context.Set<Announcement>().Add(new Announcement("Store note", "For the store", null, store.Id, Now) { Id = 201 });
        await harness.Context.SaveChangesAsync();

        var result = await CreateService(harness).GetByIdForStudentAsync(201);

        Assert.Equal(201, result.Id);
        Assert.Equal(store.StoreName, result.StoreName);
    }

    [Theory]
    [InlineData("other course")]
    [InlineData("pending enrollment")]
    [InlineData("rejected rental")]
    [InlineData("soft deleted")]
    [InlineData("missing")]
    public async Task GetByIdForStudentAsync_ThrowsNotFound_WhenAnnouncementIsNotVisible(string scenario)
    {
        var harness = await AcademicTestData.SeedAsync(CreateStudentContext(), Now);
        var announcementId = scenario switch
        {
            "other course" => 102,
            "pending enrollment" => 103,
            "rejected rental" => 104,
            "soft deleted" => 105,
            _ => 99999
        };

        if (scenario == "other course")
        {
            var secondStudent = await AcademicTestData.AddSecondInstructorContentAsync(harness, Now);
            harness.Context.Set<Announcement>().Add(new Announcement("Other course note", "Not enrolled", secondStudent.Course.Id, null, Now) { Id = announcementId });
            await harness.Context.SaveChangesAsync();
        }
        else if (scenario == "pending enrollment")
        {
            var pendingCourse = new Course("Cello pending", null, 100m, Now, Now.AddMonths(6), harness.Instructor.Id);
            harness.Context.Set<Course>().Add(pendingCourse);
            await harness.Context.SaveChangesAsync();
            harness.Context.Set<Enrollment>().Add(new Enrollment(harness.Student.Id, pendingCourse.Id, EnrollmentStatus.Pending));
            harness.Context.Set<Announcement>().Add(new Announcement("Pending course note", "Not active", pendingCourse.Id, null, Now) { Id = announcementId });
            await harness.Context.SaveChangesAsync();
        }
        else if (scenario == "rejected rental")
        {
            var store = await AddStoreWithRentalAsync(harness, harness.Student.Id, InstrumentRentalStatus.Rejected, "Store rejected");
            harness.Context.Set<Announcement>().Add(new Announcement("Rejected store note", "Not an active rental", null, store.Id, Now) { Id = announcementId });
            await harness.Context.SaveChangesAsync();
        }
        else if (scenario == "soft deleted")
        {
            var announcement = new Announcement("Deleted note", "Soft deleted", harness.Course.Id, null, Now) { Id = announcementId };
            harness.Context.Set<Announcement>().Add(announcement);
            await harness.Context.SaveChangesAsync();
            announcement.SoftDelete();
            await harness.Context.SaveChangesAsync();
        }


        var exception = await Assert.ThrowsAsync<NotFoundException>(() => CreateService(harness).GetByIdForStudentAsync(announcementId));

        Assert.Equal(Messages.AnnouncementNotFound, exception.Message);
    }

    [Fact]
    public async Task GetFeedForStudentAsync_IncludesCourseAnnouncement_WhenCanceledWithPaidTimeLeft()
    {
        var harness = await AcademicTestData.SeedAsync(CreateStudentContext(), Now);
        var enrollment = await harness.Context.Set<Enrollment>().SingleAsync(e => e.StudentId == harness.Student.Id && e.CourseId == harness.Course.Id);
        enrollment.Transition(EnrollmentTrigger.Cancel, userId: 1, now: Now);
        harness.Context.Set<Announcement>().Add(new Announcement("Course note", "For the course", harness.Course.Id, null, Now) { Id = 501 });
        await harness.Context.SaveChangesAsync();

        var service = CreateService(harness);
        var feed = await service.GetFeedForStudentAsync(new AnnouncementSearchObject { Page = 1, PageSize = 10 });
        var byId = await service.GetByIdForStudentAsync(501);

        Assert.Contains(feed.Items, a => a.Id == 501);
        Assert.Equal(501, byId.Id);
    }

    [Fact]
    public async Task GetFeedForStudentAsync_ExcludesCourseAnnouncement_WhenCanceledPaidTimeHasPassed()
    {
        var harness = await AcademicTestData.SeedAsync(CreateStudentContext(), Now.AddDays(-40));
        var enrollment = await harness.Context.Set<Enrollment>().SingleAsync(e => e.StudentId == harness.Student.Id && e.CourseId == harness.Course.Id);
        enrollment.Transition(EnrollmentTrigger.Cancel, userId: 1, now: Now);
        harness.Context.Set<Announcement>().Add(new Announcement("Course note", "For the course", harness.Course.Id, null, Now) { Id = 502 });
        await harness.Context.SaveChangesAsync();

        var service = CreateService(harness);
        var feed = await service.GetFeedForStudentAsync(new AnnouncementSearchObject { Page = 1, PageSize = 10 });

        Assert.DoesNotContain(feed.Items, a => a.Id == 502);
        var exception = await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdForStudentAsync(502));
        Assert.Equal(Messages.AnnouncementNotFound, exception.Message);
    }

    [Fact]
    public async Task GetByIdForStudentAsync_MatchesFeedVisibility()
    {
        var harness = await AcademicTestData.SeedAsync(CreateStudentContext(), Now);
        var store = await AddStoreWithRentalAsync(harness, harness.Student.Id, InstrumentRentalStatus.Completed, "Music Shop");
        harness.Context.Set<Announcement>().AddRange(
            new Announcement("Course note", "For the course", harness.Course.Id, null, Now) { Id = 301 },
            new Announcement("Store note", "For the store", null, store.Id, Now) { Id = 302 });
        await harness.Context.SaveChangesAsync();

        var service = CreateService(harness);
        var feed = await service.GetFeedForStudentAsync(new AnnouncementSearchObject { Page = 1, PageSize = 20 });

        foreach (var announcementId in new[] { 301, 302 })
        {
            var isFound = await IsFoundAsync(service, announcementId);
            Assert.Equal(feed.Items.Any(a => a.Id == announcementId), isFound);
        }
    }

    [Fact]
    public async Task GetFeedForStudentAsync_ExcludesAnnouncementsOfSoftDeletedCourse()
    {
        var harness = await AcademicTestData.SeedAsync(CreateStudentContext(), Now);
        harness.Context.Set<Announcement>().Add(new Announcement("Course note", "For the course", harness.Course.Id, null, Now) { Id = 401 });
        await harness.Context.SaveChangesAsync();

        var service = CreateService(harness);
        var feedBeforeDelete = await service.GetFeedForStudentAsync(new AnnouncementSearchObject { Page = 1, PageSize = 20 });
        Assert.Contains(feedBeforeDelete.Items, a => a.Id == 401);

        harness.Course.SoftDelete();
        await harness.Context.SaveChangesAsync();

        var feed = await service.GetFeedForStudentAsync(new AnnouncementSearchObject { Page = 1, PageSize = 20 });

        Assert.DoesNotContain(feed.Items, a => a.Id == 401);
        var exception = await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdForStudentAsync(401));
        Assert.Equal(Messages.AnnouncementNotFound, exception.Message);
    }

    [Fact]
    public async Task GetFeedForStudentAsync_ExcludesSoftDeletedAnnouncement()
    {
        var harness = await AcademicTestData.SeedAsync(CreateStudentContext(), Now);
        var announcement = new Announcement("Course note", "For the course", harness.Course.Id, null, Now) { Id = 402 };
        harness.Context.Set<Announcement>().Add(announcement);
        await harness.Context.SaveChangesAsync();

        var service = CreateService(harness);
        var feedBeforeDelete = await service.GetFeedForStudentAsync(new AnnouncementSearchObject { Page = 1, PageSize = 20 });
        Assert.Contains(feedBeforeDelete.Items, a => a.Id == 402);

        announcement.SoftDelete();
        await harness.Context.SaveChangesAsync();

        var feed = await service.GetFeedForStudentAsync(new AnnouncementSearchObject { Page = 1, PageSize = 20 });

        Assert.DoesNotContain(feed.Items, a => a.Id == 402);
    }

    private static ENoteContext CreateStudentContext()
    {
        var context = TestDbContextFactory.CreateContext(Now);
        context.ExplicitStoreId = null;
        return context;
    }

    private static StudentAnnouncementFeedService CreateService(AcademicHarness harness) =>
        new(harness.Context, new StubCurrentActor(student: harness.Student), TestMapper.Create(), new FixedClock(Now));

    private static async Task<bool> IsFoundAsync(StudentAnnouncementFeedService service, int announcementId)
    {
        try
        {
            await service.GetByIdForStudentAsync(announcementId);
            return true;
        }
        catch (NotFoundException)
        {
            return false;
        }
    }

    private static async Task<MusicStore> AddStoreWithRentalAsync(AcademicHarness harness, int studentId, InstrumentRentalStatus status, string name)
    {
        var store = new MusicStore(name, "09-17");
        harness.Context.Set<MusicStore>().Add(store);
        await harness.Context.SaveChangesAsync();

        var type = new InstrumentType { Type = $"Guitar {name}", MonthlyFee = 50m };
        harness.Context.Set<InstrumentType>().Add(type);
        await harness.Context.SaveChangesAsync();

        var instrument = new Instrument("Strat", "Fender", null, null, type.Id, store.Id);
        harness.Context.Set<Instrument>().Add(instrument);
        await harness.Context.SaveChangesAsync();

        var rental = new InstrumentRental(instrument.Id, studentId, store.Id, Now, null);
        RentalTestData.AdvanceTo(rental, status, Now);
        harness.Context.Set<InstrumentRental>().Add(rental);
        await harness.Context.SaveChangesAsync();

        return store;
    }
}
