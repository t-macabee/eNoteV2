using eNote.Application.Features.Academic.Assignments.Services;
using eNote.Application.Features.Academic.Courses.Services;
using eNote.Application.Features.Academic.Lectures.Services;
using eNote.Application.Features.Home.Services;
using eNote.Application.Features.Rentals.InstrumentRentals.Services;
using eNote.Domain.Entities.Assignments;
using eNote.Tests.TestUtils;
using Microsoft.Extensions.Logging.Abstractions;

namespace eNote.Tests.Home;

public sealed class StudentHomeServiceTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task NextLecture_IsEarliestScheduledFutureLecture()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var harness = await AcademicTestData.SeedAsync(context, Now);
        harness.Lecture.Cancel("Razlog", 1, Now);
        var sooner = await AcademicTestData.AddIndividualLectureAsync(harness, harness.Student, Now.AddDays(2));
        await AcademicTestData.AddIndividualLectureAsync(harness, harness.Student, Now.AddDays(3));
        var service = CreateService(context, harness);

        var dto = await service.GetAsync();

        Assert.Equal(sooner.Id, dto.NextLecture?.Id);
    }

    [Fact]
    public async Task NextLecture_IsNull_WithoutFutureLecture()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var harness = await AcademicTestData.SeedAsync(context, Now);
        harness.Lecture.Cancel("Razlog", 1, Now);
        await AcademicTestData.AddIndividualLectureAsync(harness, harness.Student, Now.AddDays(-1));
        var service = CreateService(context, harness);

        var dto = await service.GetAsync();

        Assert.Null(dto.NextLecture);
    }

    [Fact]
    public async Task OpenAssignments_ExcludesSubmittedAndPastDue()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var harness = await AcademicTestData.SeedAsync(context, Now);
        var open = new Assignment("Open", "Do it", Now.AddDays(2), harness.Lecture.Id);
        var submitted = new Assignment("Submitted", "Do it", Now.AddDays(3), harness.Lecture.Id);
        var pastDue = new Assignment("Past due", "Do it", Now.AddDays(-1), harness.Lecture.Id);
        context.Set<Assignment>().AddRange(open, submitted, pastDue);
        await context.SaveChangesAsync();
        var submission = new AssignmentSubmission(submitted.Id, harness.Student.Id);
        submission.Submit("submission.pdf", Now.AddHours(-1));
        context.Set<AssignmentSubmission>().Add(submission);
        await context.SaveChangesAsync();
        var service = CreateService(context, harness);

        var dto = await service.GetAsync();

        Assert.Equal(open.Id, Assert.Single(dto.OpenAssignments).Id);
    }

    [Fact]
    public async Task OpenAssignments_HoldsAtMostThree_OrderedByDueDate()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var harness = await AcademicTestData.SeedAsync(context, Now);
        var four = new Assignment("Four", "Do it", Now.AddDays(4), harness.Lecture.Id);
        var one = new Assignment("One", "Do it", Now.AddDays(1), harness.Lecture.Id);
        var three = new Assignment("Three", "Do it", Now.AddDays(3), harness.Lecture.Id);
        var two = new Assignment("Two", "Do it", Now.AddDays(2), harness.Lecture.Id);
        context.Set<Assignment>().AddRange(four, one, three, two);
        await context.SaveChangesAsync();
        var service = CreateService(context, harness);

        var dto = await service.GetAsync();

        Assert.Equal(new[] { one.Id, two.Id, three.Id }, dto.OpenAssignments.Select(a => a.Id));
    }

    [Fact]
    public async Task CurrentRentals_HoldsApprovedAndActiveRentalsOnly()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var harness = await AcademicTestData.SeedAsync(context, Now);
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var approvedInstrument = await AddInstrumentAsync(context, instrument, "Approved model");
        var activeInstrument = await AddInstrumentAsync(context, instrument, "Active model");
        var requested = new InstrumentRental(instrument.Id, harness.Student.Id, instrument.MusicStoreId, Now, null);
        var approved = new InstrumentRental(approvedInstrument.Id, harness.Student.Id, instrument.MusicStoreId, Now.AddDays(-2), null);
        RentalTestData.AdvanceTo(approved, InstrumentRentalStatus.Approved, Now, approvedAt: Now.AddDays(-1));
        var active = new InstrumentRental(activeInstrument.Id, harness.Student.Id, instrument.MusicStoreId, Now.AddDays(-3), null);
        RentalTestData.AdvanceTo(active, InstrumentRentalStatus.Active, Now, approvedAt: Now.AddDays(-2), pickedUpAt: Now.AddDays(-1));
        context.Set<InstrumentRental>().AddRange(requested, approved, active);
        await context.SaveChangesAsync();
        var service = CreateService(context, harness);

        var dto = await service.GetAsync();

        Assert.Equal(new[] { approved.Id, active.Id }, dto.CurrentRentals.Select(r => r.Id));
    }

    [Fact]
    public async Task TuitionDueCourses_HoldsActivePaidCoursesWithUnpaidOrExpiredTuition()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var harness = await AcademicTestData.SeedAsync(context, Now);
        var unpaid = await AddEnrolledCourseAsync(context, harness, "Unpaid", 100m, paidFrom: null);
        var expired = await AddEnrolledCourseAsync(context, harness, "Expired", 100m, paidFrom: Now.AddDays(-60));
        var service = CreateService(context, harness);

        var dto = await service.GetAsync();

        Assert.Equal(new[] { unpaid.Id, expired.Id }.Order(), dto.TuitionDueCourses.Select(c => c.Id).Order());
    }

    [Fact]
    public async Task TuitionDueCourses_ExcludesFreeCourses()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var harness = await AcademicTestData.SeedAsync(context, Now);
        await AddEnrolledCourseAsync(context, harness, "Free", 0m, paidFrom: null);
        var service = CreateService(context, harness);

        var dto = await service.GetAsync();

        Assert.Empty(dto.TuitionDueCourses);
    }

    [Fact]
    public async Task Debt_ReturnsUnpaidRental()
    {
        await using var context = RentalTestData.CreateContext(Now);
        var harness = await AcademicTestData.SeedAsync(context, Now);
        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var unpaid = new InstrumentRental(instrument.Id, harness.Student.Id, instrument.MusicStoreId, Now.AddDays(-20), null);
        RentalTestData.AdvanceTo(unpaid, InstrumentRentalStatus.Completed, Now, approvedAt: Now.AddDays(-35), pickedUpAt: Now.AddDays(-35), finishedAt: Now.AddDays(-5));
        context.Set<InstrumentRental>().Add(unpaid);
        await context.SaveChangesAsync();
        var service = CreateService(context, harness);

        var dto = await service.GetAsync();

        Assert.True(dto.Debt.HasUnpaidDebt);
        Assert.Equal(unpaid.Id, dto.Debt.RentalId);
    }

    private static async Task<Course> AddEnrolledCourseAsync(ENoteContext context, AcademicHarness harness, string name, decimal price, DateTime? paidFrom)
    {
        var course = new Course(name, null, price, Now, Now.AddMonths(6), harness.Instructor.Id)
        {
            CreatedById = harness.Instructor.AppUserId
        };
        course.SetPublishedStatus(true);
        context.Set<Course>().Add(course);
        await context.SaveChangesAsync();

        var enrollment = new Enrollment(harness.Student.Id, course.Id, EnrollmentStatus.Active);
        if (paidFrom.HasValue)
        {
            enrollment.ExtendPaidUntil(paidFrom.Value, 30);
        }

        context.Set<Enrollment>().Add(enrollment);
        await context.SaveChangesAsync();

        return course;
    }

    private static async Task<Instrument> AddInstrumentAsync(ENoteContext context, Instrument template, string model)
    {
        var instrument = new Instrument(model, "Second Manufacturer", null, null, template.InstrumentTypeId, template.MusicStoreId);
        context.Set<Instrument>().Add(instrument);
        await context.SaveChangesAsync();
        return instrument;
    }

    private static StudentHomeService CreateService(ENoteContext context, AcademicHarness harness)
    {
        var actor = new StubCurrentActor(student: harness.Student, storeId: 1);
        var clock = new FixedClock(Now);
        var mapper = TestMapper.Create();
        var instructorAccess = AcademicTestData.CreateInstructorAccess(context, harness.Instructor);

        var lectures = new LectureService(
            context,
            actor,
            actor,
            instructorAccess,
            new RecordingLectureNotificationDispatcher(),
            new StubDisplayNameService(),
            NullLogger<LectureService>.Instance,
            mapper,
            clock);
        var assignments = new AssignmentService(context, actor, actor, instructorAccess, mapper, clock);
        var rentals = new RentalQueryService(context, mapper, actor, clock, new StubDisplayNameService());
        var courses = new CourseService(
            context,
            mapper,
            actor,
            actor,
            instructorAccess,
            NullLogger<CourseService>.Instance,
            new StubUserIdentityService(),
            new FakePaymentGateway(),
            clock);

        return new StudentHomeService(lectures, assignments, rentals, courses, clock);
    }
}
