using eNote.Application.Features.Academic.Assignments.Services;
using eNote.Application.Features.Academic.Courses.Services;
using eNote.Application.Features.Academic.Lectures.Services;
using eNote.Application.Features.Home.Services;
using eNote.Application.Features.Rentals.InstrumentRentals;
using eNote.Application.Features.Rentals.InstrumentRentals.Services;
using eNote.Application.Features.Rentals.Instruments;
using eNote.Application.Features.Rentals.Instruments.Services;
using eNote.Infrastructure.Identity;
using eNote.Infrastructure.Messaging;
using eNote.Tests.TestUtils;
using Microsoft.Extensions.Logging.Abstractions;

namespace eNote.Tests.Integration;

[Collection(SqlServerCollection.Name)]
public sealed class QueryCountTests(SqlServerFixture fixture)
{
    private static readonly DateTime Now = new(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task StudentHome_FiltersTuitionDueInTheDatabase_AndRunsOneRentalQuery()
    {
        var database = await fixture.CreateDatabaseAsync();
        var interceptor = new CountingCommandInterceptor();
        await using var context = database.CreateContext(Now, new StubCurrentActor(isAuthenticated: false), interceptor);

        var instructor = await SeedInstructorAsync(context);
        var student = await SeedStudentAsync(context);
        var unpaid = await AddEnrolledCourseAsync(context, instructor, student, "Unpaid", 100m, paid: false);
        await AddEnrolledCourseAsync(context, instructor, student, "Paid one", 100m, paid: true);
        await AddEnrolledCourseAsync(context, instructor, student, "Paid two", 80m, paid: true);

        var instrument = await RentalTestData.SeedInstrumentAsync(context);
        var approvedInstrument = await AddInstrumentAsync(context, instrument, "Approved model");
        var activeInstrument = await AddInstrumentAsync(context, instrument, "Active model");
        var approved = new InstrumentRental(approvedInstrument.Id, student.Id, approvedInstrument.MusicStoreId, Now.AddDays(-2), null);
        RentalTestData.AdvanceTo(approved, InstrumentRentalStatus.Approved, Now, approvedAt: Now.AddDays(-1));
        var active = new InstrumentRental(activeInstrument.Id, student.Id, activeInstrument.MusicStoreId, Now.AddDays(-3), null);
        RentalTestData.AdvanceTo(active, InstrumentRentalStatus.Active, Now, approvedAt: Now.AddDays(-2), pickedUpAt: Now.AddDays(-1));
        context.Set<InstrumentRental>().AddRange(approved, active);
        await context.SaveChangesAsync();

        interceptor.Clear();
        var service = CreateHomeService(context, student, instructor);

        var dto = await service.GetAsync();

        Assert.Equal(unpaid.Id, Assert.Single(dto.TuitionDueCourses).Id);
        Assert.Equal(new[] { approved.Id, active.Id }, dto.CurrentRentals.Select(x => x.Id));

        var courseQuery = Assert.Single(interceptor.Commands, sql => sql.Contains("ORDER BY [c].[StartDate] DESC"));
        Assert.Contains("[PaidUntil] IS NULL OR", courseQuery);
        Assert.Contains("[c].[Price] <> 0.0", courseQuery);

        Assert.Equal(1, interceptor.CountContaining("ORDER BY [i].[RequestedAt] DESC"));
    }

    [Fact]
    public async Task CancelPendingForClosedAccountAsync_LoadsStoreStaffOnce()
    {
        var database = await fixture.CreateDatabaseAsync();
        var interceptor = new CountingCommandInterceptor();
        await using var context = database.CreateContext(Now, new StubCurrentActor(isAuthenticated: false), interceptor);

        var student = await SeedStudentAsync(context);
        var employee = new AppUser
        {
            UserName = "employee",
            NormalizedUserName = "EMPLOYEE",
            Email = "employee@example.com",
            NormalizedEmail = "EMPLOYEE@EXAMPLE.COM",
            IsActive = true,
            FirstName = "Test",
            LastName = "User"
        };
        context.Set<AppUser>().Add(employee);
        var store = new MusicStore("Store", "09-17");
        context.Set<MusicStore>().Add(store);
        await context.SaveChangesAsync();

        context.Set<MusicStoreEmployee>().Add(new MusicStoreEmployee(employee.Id, store.Id, isManager: true));
        var type = new InstrumentType { Type = "Guitar", MonthlyFee = 50m };
        context.Set<InstrumentType>().Add(type);
        await context.SaveChangesAsync();

        for (var index = 0; index < 3; index++)
        {
            var instrument = new Instrument($"Model {index}", TestManufacturers.Id("Fender"), null, null, type.Id, store.Id);
            context.Set<Instrument>().Add(instrument);
            await context.SaveChangesAsync();
            context.Set<InstrumentRental>().Add(new InstrumentRental(instrument.Id, student.Id, store.Id, Now, null));
        }

        await context.SaveChangesAsync();

        interceptor.Clear();
        var clock = new FixedClock(Now);
        var dispatcher = new RentalNotificationDispatcher(context, clock);
        var actor = new StubCurrentActor(userId: student.AppUserId);
        var service = new RentalCommandService(
            context,
            TestMapper.Create(),
            clock,
            actor,
            new StubCurrentActor(student: student),
            new StubCurrentActor(storeId: store.Id),
            dispatcher,
            new StubDisplayNameService(),
            new RentalOptions { OverduePenaltyMultiplier = 0.5m });

        await service.CancelPendingForClosedAccountAsync(student.Id, student.AppUserId);
        await context.SaveChangesAsync();

        Assert.Equal(1, interceptor.CountContaining("FROM [MusicStoreEmployee]"));
    }

    [Fact]
    public async Task CreateAsync_BuildsTheDtoWithoutQueryingTheInstrumentAgain()
    {
        var database = await fixture.CreateDatabaseAsync();
        var interceptor = new CountingCommandInterceptor();
        await using var context = database.CreateContext(Now, new StubCurrentActor(isAuthenticated: false), interceptor);

        var store = new MusicStore("Music Shop Sarajevo", "09-17");
        context.Set<MusicStore>().Add(store);
        var type = new InstrumentType { Type = "Guitar", MonthlyFee = 50m };
        context.Set<InstrumentType>().Add(type);
        await context.SaveChangesAsync();

        var service = CreateInstrumentService(context, store);

        interceptor.Clear();
        var dto = await service.CreateAsync(new InstrumentRequest
        {
            Model = "Stratocaster",
            ManufacturerId = TestManufacturers.Id("Fender"),
            InstrumentTypeId = type.Id
        });

        Assert.Equal("Guitar", dto.InstrumentType);
        Assert.Equal("Fender", dto.ManufacturerName);
        Assert.Equal("Music Shop Sarajevo", dto.MusicStore);
        Assert.Equal(0, interceptor.CountContaining("FROM [Instrument]"));
    }

    [Fact]
    public async Task UpdateAsync_BuildsTheDtoWithoutQueryingTheStoreAgain()
    {
        var database = await fixture.CreateDatabaseAsync();
        var interceptor = new CountingCommandInterceptor();
        await using var context = database.CreateContext(Now, new StubCurrentActor(isAuthenticated: false), interceptor);
        var (store, type, instrument) = await SeedStoreInstrumentAsync(context);
        var service = CreateInstrumentService(context, store);

        interceptor.Clear();
        var dto = await service.UpdateAsync(instrument.Id, new InstrumentRequest
        {
            Model = "Telecaster",
            ManufacturerId = TestManufacturers.Id("Fender"),
            InstrumentTypeId = type.Id
        });

        Assert.Equal("Telecaster", dto.Model);
        Assert.Equal("Guitar", dto.InstrumentType);
        Assert.Equal("Fender", dto.ManufacturerName);
        Assert.Equal("Music Shop Sarajevo", dto.MusicStore);
        Assert.True(dto.IsAvailable);
        Assert.Null(dto.DeleteBlockedReason);
        Assert.Equal(0, interceptor.CountContaining("FROM [MusicStore]"));
    }

    [Fact]
    public async Task UploadImageAsync_LoadsTheTypeManufacturerAndStoreWithTheInstrument()
    {
        var database = await fixture.CreateDatabaseAsync();
        var interceptor = new CountingCommandInterceptor();
        await using var context = database.CreateContext(Now, new StubCurrentActor(isAuthenticated: false), interceptor);
        var (store, _, instrument) = await SeedStoreInstrumentAsync(context);
        var service = CreateInstrumentService(context, store);

        interceptor.Clear();
        var dto = await service.UploadImageAsync(instrument.Id, new MemoryStream([1, 2, 3]), "image/png");

        Assert.Equal("Guitar", dto.InstrumentType);
        Assert.Equal("Fender", dto.ManufacturerName);
        Assert.Equal("Music Shop Sarajevo", dto.MusicStore);
        Assert.Equal(0, interceptor.CountContaining("FROM [InstrumentType]"));
        Assert.Equal(0, interceptor.CountContaining("FROM [Manufacturer]"));
        Assert.Equal(1, interceptor.CountContaining("FROM [MusicStore]"));
    }

    private static async Task<(MusicStore Store, InstrumentType Type, Instrument Instrument)> SeedStoreInstrumentAsync(ENoteContext context)
    {
        var store = new MusicStore("Music Shop Sarajevo", "09-17");
        context.Set<MusicStore>().Add(store);
        var type = new InstrumentType { Type = "Guitar", MonthlyFee = 50m };
        context.Set<InstrumentType>().Add(type);
        await context.SaveChangesAsync();

        var instrument = new Instrument("Stratocaster", TestManufacturers.Id("Fender"), null, null, type.Id, store.Id);
        context.Set<Instrument>().Add(instrument);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        return (store, type, instrument);
    }

    private static InstrumentService CreateInstrumentService(ENoteContext context, MusicStore store)
    {
        var actor = new StubCurrentActor(storeId: store.Id, employee: new MusicStoreEmployee(1, store.Id, isManager: true));

        return new InstrumentService(
            context,
            actor,
            new StubFileStorageService(),
            new RentalCommandService(
                context,
                TestMapper.Create(),
                new FixedClock(Now),
                actor,
                actor,
                actor,
                new RecordingNotificationDispatcher(),
                new StubDisplayNameService(),
                new RentalOptions { OverduePenaltyMultiplier = 0.5m }));
    }

    private static async Task<Instructor> SeedInstructorAsync(ENoteContext context)
    {
        var user = new AppUser
        {
            UserName = "instructor",
            NormalizedUserName = "INSTRUCTOR",
            Email = "instructor@example.com",
            NormalizedEmail = "INSTRUCTOR@EXAMPLE.COM",
            IsActive = true,
            FirstName = "Test",
            LastName = "User"
        };
        context.Set<AppUser>().Add(user);
        await context.SaveChangesAsync();
        var instructor = new Instructor(user.Id);
        context.Set<Instructor>().Add(instructor);
        await context.SaveChangesAsync();
        return instructor;
    }

    private static async Task<Student> SeedStudentAsync(ENoteContext context)
    {
        var user = new AppUser
        {
            UserName = "student",
            NormalizedUserName = "STUDENT",
            Email = "student@example.com",
            NormalizedEmail = "STUDENT@EXAMPLE.COM",
            IsActive = true,
            FirstName = "Test",
            LastName = "User"
        };
        context.Set<AppUser>().Add(user);
        await context.SaveChangesAsync();
        var student = new Student(user.Id, Now.AddMonths(-1));
        student.UpdateMembership(Now.AddDays(30));
        context.Set<Student>().Add(student);
        await context.SaveChangesAsync();
        return student;
    }

    private static async Task<Course> AddEnrolledCourseAsync(
        ENoteContext context,
        Instructor instructor,
        Student student,
        string name,
        decimal price,
        bool paid)
    {
        var course = new Course(name, null, price, Now, Now.AddMonths(6), instructor.Id)
        {
            CreatedById = instructor.AppUserId
        };
        course.SetPublishedStatus(true);
        context.Set<Course>().Add(course);
        await context.SaveChangesAsync();

        var enrollment = new Enrollment(student.Id, course.Id, EnrollmentStatus.Active);
        if (paid)
        {
            enrollment.ExtendPaidUntil(Now, 30);
        }

        context.Set<Enrollment>().Add(enrollment);
        await context.SaveChangesAsync();
        return course;
    }

    private static async Task<Instrument> AddInstrumentAsync(ENoteContext context, Instrument reference, string model)
    {
        var instrument = new Instrument(
            model,
            TestManufacturers.Id("Second Manufacturer"),
            null,
            null,
            reference.InstrumentTypeId,
            reference.MusicStoreId);
        context.Set<Instrument>().Add(instrument);
        await context.SaveChangesAsync();
        return instrument;
    }

    private static StudentHomeService CreateHomeService(ENoteContext context, Student student, Instructor instructor)
    {
        var actor = new StubCurrentActor(student: student, storeId: 1);
        var clock = new FixedClock(Now);
        var mapper = TestMapper.Create();
        var instructorAccess = AcademicTestData.CreateInstructorAccess(context, instructor);

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
