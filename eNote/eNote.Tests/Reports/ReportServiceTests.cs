using eNote.Application.Features.Academic.Courses.Services;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Application.Features.Payments;
using eNote.Domain.Entities.Assignments;
using eNote.Infrastructure.Reports;
using eNote.Tests.TestUtils;
using Microsoft.EntityFrameworkCore;

namespace eNote.Tests.Reports;

public sealed class ReportServiceTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);
    private const string NoDataLine = "Nema podataka za odabrane kriterije.";
    private const string FooterLine = "Generisano: 01.07.2026 12:00 UTC Stranica 1 od 1";

    [Fact]
    public async Task GenerateCourseRankingPdfAsync_ListsEveryGradedStudentByAverageThenStudentId()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var other = await AcademicTestData.AddEnrolledStudentAsync(harness, 60, Now);
        var third = await AcademicTestData.AddEnrolledStudentAsync(harness, 61, Now);
        var ungraded = await AcademicTestData.AddEnrolledStudentAsync(harness, 62, Now);
        var first = await AddAssignmentAsync(harness, "Homework 1");
        var second = await AddAssignmentAsync(harness, "Homework 2");
        await AddGradeAsync(harness, first, harness.Student, 90);
        await AddGradeAsync(harness, second, harness.Student, 80);
        await AddGradeAsync(harness, first, other, 100);
        await AddGradeAsync(harness, first, third, 85);
        await AddGradeAsync(harness, second, third, 85);
        var service = CreateService(harness, harness.Instructor);

        var lines = PdfText.Lines(await service.GenerateCourseRankingPdfAsync(harness.Course.Id));

        Assert.Equal(
        [
            "Rang lista — Guitar 101",
            "Rang Student Prosjek Ocijenjeno",
            $"1 Student {other.Id} 100,00 1",
            $"2 Student {harness.Student.Id} 85,00 2",
            $"3 Student {third.Id} 85,00 2",
            "Ukupno ocijenjenih studenata: 3",
            FooterLine
        ], lines);
        Assert.DoesNotContain(lines, line => line.Contains($"Student {ungraded.Id}"));
    }

    [Fact]
    public async Task GenerateCourseRankingPdfAsync_IncludesStudentsBeyondTheTopFifteen()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var assignment = await AddAssignmentAsync(harness, "Homework 1");
        var students = new List<Student> { harness.Student };
        for (var i = 1; i < 17; i++)
        {
            students.Add(await AcademicTestData.AddEnrolledStudentAsync(harness, 100 + i, Now));
        }

        for (var i = 0; i < students.Count; i++)
        {
            await AddGradeAsync(harness, assignment, students[i], 60 + i);
        }

        var service = CreateService(harness, harness.Instructor);

        var lines = PdfText.Lines(await service.GenerateCourseRankingPdfAsync(harness.Course.Id));

        Assert.Contains($"1 Student {students[16].Id} 76,00 1", lines);
        Assert.Contains($"17 Student {harness.Student.Id} 60,00 1", lines);
        Assert.Contains("Ukupno ocijenjenih studenata: 17", lines);
    }

    [Fact]
    public async Task GenerateCourseRankingPdfAsync_ShowsTheNoDataLine_WhenNobodyIsGraded()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var service = CreateService(harness, harness.Instructor);

        var lines = PdfText.Lines(await service.GenerateCourseRankingPdfAsync(harness.Course.Id));

        Assert.Equal(["Rang lista — Guitar 101", NoDataLine, FooterLine], lines);
    }

    [Fact]
    public async Task GenerateCourseRankingPdfAsync_Throws_ForCourseOfAnotherInstructor()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var otherInstructor = new Instructor(300);
        harness.Context.Set<Instructor>().Add(otherInstructor);
        await harness.Context.SaveChangesAsync();
        var service = CreateService(harness, otherInstructor);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GenerateCourseRankingPdfAsync(harness.Course.Id));
    }

    [Fact]
    public async Task GenerateLectureAttendancePdfAsync_ShowsHeaderRowsAndTotals()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var absent = await AcademicTestData.AddEnrolledStudentAsync(harness, 60, Now);
        var pending = await AcademicTestData.AddEnrolledStudentAsync(harness, 61, Now);
        var present = await AcademicTestData.AddEnrolledStudentAsync(harness, 62, Now);
        harness.Context.Set<Attendance>().AddRange(
            new Attendance(harness.Student.Id, harness.Lecture.Id, AttendanceStatus.Present),
            new Attendance(present.Id, harness.Lecture.Id, AttendanceStatus.Present),
            new Attendance(absent.Id, harness.Lecture.Id, AttendanceStatus.Absent),
            new Attendance(pending.Id, harness.Lecture.Id, AttendanceStatus.Pending));
        await harness.Context.SaveChangesAsync();
        var names = new NamedDisplayNames(new()
        {
            [harness.Student.Id] = "Ana Anić",
            [present.Id] = "Bojan Bošnjak",
            [absent.Id] = "Cvijeta Cvijić",
            [pending.Id] = "Dino Delić"
        });
        var service = CreateService(harness, harness.Instructor, names: names);

        var lines = PdfText.Lines(await service.GenerateLectureAttendancePdfAsync(harness.Lecture.Id));

        Assert.Equal(
        [
            "Prisustvo — First lesson",
            "01.07.2026 12:00 · Room 1",
            "Student Status",
            "Ana Anić Prisutan",
            "Bojan Bošnjak Prisutan",
            "Cvijeta Cvijić Odsutan",
            "Dino Delić Na čekanju",
            "Prisutno: 2 · Odsutno: 1 · Na čekanju: 1 · Ukupno: 4",
            FooterLine
        ], lines);
    }

    [Fact]
    public async Task GenerateLectureAttendancePdfAsync_SortsNamesInTheBosnianAlphabet()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        string[] shuffled = ["Zlatan", "Željko", "Šaban", "Sanela", "Čedomir", "Ćamil", "Cemal", "Đula", "Dino", "Dženan"];
        var names = new Dictionary<int, string>();
        for (var i = 0; i < shuffled.Length; i++)
        {
            var student = await AcademicTestData.AddEnrolledStudentAsync(harness, 200 + i, Now);
            harness.Context.Set<Attendance>().Add(new Attendance(student.Id, harness.Lecture.Id, AttendanceStatus.Present));
            names[student.Id] = shuffled[i];
        }

        await harness.Context.SaveChangesAsync();
        var service = CreateService(harness, harness.Instructor, names: new NamedDisplayNames(names));

        var lines = PdfText.Lines(await service.GenerateLectureAttendancePdfAsync(harness.Lecture.Id));

        var listed = lines.Where(line => line.EndsWith(" Prisutan", StringComparison.Ordinal)).Select(line => line[..^" Prisutan".Length]);
        Assert.Equal(["Cemal", "Čedomir", "Ćamil", "Dino", "Dženan", "Đula", "Sanela", "Šaban", "Zlatan", "Željko"], listed);
    }

    [Fact]
    public async Task GenerateLectureAttendancePdfAsync_OrdersStudentsWithTheSameNameByStudentId()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var later = await AcademicTestData.AddEnrolledStudentAsync(harness, 60, Now);
        harness.Context.Set<Attendance>().Add(new Attendance(later.Id, harness.Lecture.Id, AttendanceStatus.Absent));
        await harness.Context.SaveChangesAsync();
        harness.Context.Set<Attendance>().Add(new Attendance(harness.Student.Id, harness.Lecture.Id, AttendanceStatus.Present));
        await harness.Context.SaveChangesAsync();
        var names = new NamedDisplayNames(new() { [harness.Student.Id] = "Ana Anić", [later.Id] = "Ana Anić" });
        var service = CreateService(harness, harness.Instructor, names: names);

        var lines = PdfText.Lines(await service.GenerateLectureAttendancePdfAsync(harness.Lecture.Id));

        var listed = lines.Where(line => line.StartsWith("Ana Anić ", StringComparison.Ordinal));
        Assert.Equal(["Ana Anić Prisutan", "Ana Anić Odsutan"], listed);
    }

    [Fact]
    public async Task GenerateLectureAttendancePdfAsync_ListsOnlyTheAssignedStudent_ForIndividualLecture()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var classmate = await AcademicTestData.AddEnrolledStudentAsync(harness, 60, Now);
        var lecture = await AcademicTestData.AddIndividualLectureAsync(harness, harness.Student, Now.AddDays(1));
        harness.Context.Set<Attendance>().AddRange(
            new Attendance(harness.Student.Id, lecture.Id, AttendanceStatus.Present),
            new Attendance(classmate.Id, lecture.Id, AttendanceStatus.Present));
        await harness.Context.SaveChangesAsync();
        var service = CreateService(harness, harness.Instructor);

        var lines = PdfText.Lines(await service.GenerateLectureAttendancePdfAsync(lecture.Id));

        Assert.Contains($"Student {harness.Student.Id} Prisutan", lines);
        Assert.DoesNotContain(lines, line => line.Contains($"Student {classmate.Id}"));
        Assert.Contains("Prisutno: 1 · Odsutno: 0 · Na čekanju: 0 · Ukupno: 1", lines);
    }

    [Fact]
    public async Task GenerateLectureAttendancePdfAsync_ShowsTheNoDataLine_WhenNobodyHasAttendance()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var service = CreateService(harness, harness.Instructor);

        var lines = PdfText.Lines(await service.GenerateLectureAttendancePdfAsync(harness.Lecture.Id));

        Assert.Equal(["Prisustvo — First lesson", "01.07.2026 12:00 · Room 1", NoDataLine, FooterLine], lines);
    }

    [Fact]
    public async Task GenerateLectureAttendancePdfAsync_Throws_ForLectureOfAnotherInstructor()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var otherInstructor = new Instructor(300);
        harness.Context.Set<Instructor>().Add(otherInstructor);
        await harness.Context.SaveChangesAsync();
        var service = CreateService(harness, otherInstructor);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GenerateLectureAttendancePdfAsync(harness.Lecture.Id));
    }

    [Fact]
    public async Task GenerateStoreRentalSummaryPdfAsync_ShowsStudentStatusPeriodAmountsAndTotal_NewestRequestFirst()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var (store, type) = await AddStoreAsync(harness.Context, "Music Shop");
        var completed = await AddInstrumentAsync(harness.Context, store, type, "M-Done");
        var pending = await AddInstrumentAsync(harness.Context, store, type, "M-Wait");
        var active = await AddInstrumentAsync(harness.Context, store, type, "M-Live");
        var completedRental = new InstrumentRental(completed.Id, harness.Student.Id, store.Id, Now.AddDays(-35), null);
        RentalTestData.AdvanceTo(completedRental, InstrumentRentalStatus.Completed, Now, fee: 60m, approvedAt: Now.AddDays(-34), pickedUpAt: Now.AddDays(-33), finishedAt: Now);
        var pendingRental = new InstrumentRental(pending.Id, harness.Student.Id, store.Id, Now.AddDays(-1), null);
        var activeRental = new InstrumentRental(active.Id, harness.Student.Id, store.Id, Now.AddDays(-3), null);
        RentalTestData.AdvanceTo(activeRental, InstrumentRentalStatus.Active, Now, fee: 50m, approvedAt: Now.AddDays(-2), pickedUpAt: Now.AddDays(-2));
        harness.Context.Set<InstrumentRental>().AddRange(completedRental, pendingRental, activeRental);
        await harness.Context.SaveChangesAsync();
        var names = new NamedDisplayNames(new() { [harness.Student.Id] = "Šaban Čolić" });
        var service = CreateService(harness, harness.Instructor, new StubCurrentActor(storeId: store.Id), names);

        var lines = PdfText.Lines(await service.GenerateStoreRentalSummaryPdfAsync());

        Assert.Equal(
        [
            "Pregled iznajmljivanja — Music Shop",
            "Iznosi su u valuti: BAM",
            "Instrument Student Status Period Naknada Penal Ukupno",
            "M-Wait Šaban Čolić Na čekanju - - - -",
            "M-Live Šaban Čolić Aktivno 29.06.2026 – 50,00 0,00 3,33",
            "M-Done Šaban Čolić Završeno 29.05.2026 – 01.07.2026 60,00 3,00 69,00",
            "Ukupno iznajmljivanja: 3 · Obračunati iznos: 72,33 BAM",
            FooterLine
        ], lines);
    }

    [Fact]
    public async Task GenerateStoreRentalSummaryPdfAsync_ListsOnlyRentalsOfTheCurrentStore_WithoutHelpFromTheGlobalFilter()
    {
        var unfiltered = new ENoteContext(TestDbContextFactory.CreateOptions(), new FixedClock(Now), new StubCurrentActor(new Student(0, Now)));
        var harness = await AcademicTestData.SeedAsync(unfiltered, Now);
        var (store, type) = await AddStoreAsync(harness.Context, "Music Shop");
        var foreignStore = new MusicStore("Foreign Shop", "09-17");
        harness.Context.Set<MusicStore>().Add(foreignStore);
        await harness.Context.SaveChangesAsync();
        var own = await AddInstrumentAsync(harness.Context, store, type, "Own-Model");
        var foreign = await AddInstrumentAsync(harness.Context, foreignStore, type, "Foreign-Model");
        harness.Context.Set<InstrumentRental>().AddRange(
            new InstrumentRental(own.Id, harness.Student.Id, store.Id, Now.AddDays(-1), null),
            new InstrumentRental(foreign.Id, harness.Student.Id, foreignStore.Id, Now.AddDays(-1), null));
        await harness.Context.SaveChangesAsync();
        var service = CreateService(harness, harness.Instructor, new StubCurrentActor(storeId: store.Id));

        var lines = PdfText.Lines(await service.GenerateStoreRentalSummaryPdfAsync());

        Assert.Contains(lines, line => line.StartsWith("Own-Model ", StringComparison.Ordinal));
        Assert.DoesNotContain(lines, line => line.Contains("Foreign-Model"));
        Assert.Contains("Ukupno iznajmljivanja: 1 · Obračunati iznos: 0,00 BAM", lines);
    }

    [Fact]
    public async Task GenerateStoreRentalSummaryPdfAsync_OrdersRentalsWithTheSameRequestTimeByNewestId()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var (store, type) = await AddStoreAsync(harness.Context, "Music Shop");
        var older = await AddInstrumentAsync(harness.Context, store, type, "Tie-First");
        var newer = await AddInstrumentAsync(harness.Context, store, type, "Tie-Second");
        harness.Context.Set<InstrumentRental>().Add(new InstrumentRental(older.Id, harness.Student.Id, store.Id, Now.AddDays(-1), null));
        await harness.Context.SaveChangesAsync();
        harness.Context.Set<InstrumentRental>().Add(new InstrumentRental(newer.Id, harness.Student.Id, store.Id, Now.AddDays(-1), null));
        await harness.Context.SaveChangesAsync();
        var service = CreateService(harness, harness.Instructor, new StubCurrentActor(storeId: store.Id));

        var lines = PdfText.Lines(await service.GenerateStoreRentalSummaryPdfAsync());

        var models = lines.Where(line => line.StartsWith("Tie-", StringComparison.Ordinal)).Select(line => line.Split(' ')[0]);
        Assert.Equal(["Tie-Second", "Tie-First"], models);
    }

    [Fact]
    public async Task GenerateStoreRentalSummaryPdfAsync_ShowsTheNoDataLine_WhenTheStoreHasNoRentals()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var (store, _) = await AddStoreAsync(harness.Context, "Music Shop");
        var service = CreateService(harness, harness.Instructor, new StubCurrentActor(storeId: store.Id));

        var lines = PdfText.Lines(await service.GenerateStoreRentalSummaryPdfAsync());

        Assert.Equal(["Pregled iznajmljivanja — Music Shop", "Iznosi su u valuti: BAM", NoDataLine, FooterLine], lines);
    }

    [Fact]
    public async Task GenerateStoreRentalSummaryPdfAsync_Throws_WhenStoreNotResolved()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var service = CreateService(harness, harness.Instructor, new ThrowingStoreContext());

        await Assert.ThrowsAsync<StoreNotResolvedException>(() => service.GenerateStoreRentalSummaryPdfAsync());
    }

    [Fact]
    public async Task LoadRentalSummaryRowsAsync_IncludesRentalsOfArchivedInstruments()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var instrument = await RentalTestData.SeedInstrumentAsync(harness.Context);
        var rental = RentalTestData.CreateCompletedRental(instrument, harness.Student.Id, Now);
        harness.Context.Set<InstrumentRental>().Add(rental);
        await harness.Context.SaveChangesAsync();

        instrument.SoftDelete();
        await harness.Context.SaveChangesAsync();
        var service = CreateService(harness, harness.Instructor, new StubCurrentActor(storeId: instrument.MusicStoreId));

        var rows = await service.LoadRentalSummaryRowsAsync(instrument.MusicStoreId, CancellationToken.None);

        var row = Assert.Single(rows);
        Assert.Equal("Stradivarius", row.InstrumentModel);
        Assert.Equal(50.00m, row.CalculateCharges(Now).TotalFee);
    }

    [Fact]
    public async Task GenerateAdminMusicStoreReportAsync_ListsEveryStoreWithCityAndAddress()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var city = new City { Name = "Mostar" };
        harness.Context.Set<City>().Add(city);
        await harness.Context.SaveChangesAsync();
        var address = new Address { CityId = city.Id, Street = "Maršala Tita", Number = "5" };
        harness.Context.Set<Address>().Add(address);
        await harness.Context.SaveChangesAsync();
        harness.Context.Set<MusicStore>().AddRange(new MusicStore("Beta Muzika", "09:00-17:00"), new MusicStore("Alfa Muzika", "08:00-16:00", address.Id));
        await harness.Context.SaveChangesAsync();
        var service = CreateService(harness, harness.Instructor);

        var lines = PdfText.Lines(await service.GenerateAdminMusicStoreReportAsync());

        Assert.Equal(
        [
            "Izvještaj — Muzičke prodavnice",
            "Naziv Grad Adresa Radno vrijeme",
            "Alfa Muzika Mostar Maršala Tita 5 08:00-16:00",
            "Beta Muzika - - 09:00-17:00",
            "Ukupno prodavnica: 2",
            FooterLine
        ], lines);
    }

    [Fact]
    public async Task GenerateAdminMusicStoreReportAsync_ShowsTheNoDataLine_WhenThereAreNoStores()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var service = CreateService(harness, harness.Instructor);

        var lines = PdfText.Lines(await service.GenerateAdminMusicStoreReportAsync());

        Assert.Equal(["Izvještaj — Muzičke prodavnice", NoDataLine, FooterLine], lines);
    }

    [Theory]
    [InlineData(InstrumentRentalStatus.Pending, "Na čekanju")]
    [InlineData(InstrumentRentalStatus.Approved, "Odobreno")]
    [InlineData(InstrumentRentalStatus.Active, "Aktivno")]
    [InlineData(InstrumentRentalStatus.Completed, "Završeno")]
    [InlineData(InstrumentRentalStatus.Rejected, "Odbijeno")]
    [InlineData(InstrumentRentalStatus.Canceled, "Otkazano")]
    [InlineData(InstrumentRentalStatus.ReturnedEarly, "Prijevremeni povrat")]
    public void ReportLabels_Rental_MatchesTheDesktopAppWording(InstrumentRentalStatus status, string expected)
    {
        Assert.Equal(expected, ReportLabels.Rental(status));
    }

    [Theory]
    [InlineData(AttendanceStatus.Pending, "Na čekanju")]
    [InlineData(AttendanceStatus.Present, "Prisutan")]
    [InlineData(AttendanceStatus.Absent, "Odsutan")]
    public void ReportLabels_Attendance_MatchesTheDesktopAppWording(AttendanceStatus status, string expected)
    {
        Assert.Equal(expected, ReportLabels.Attendance(status));
    }

    [Fact]
    public void ReportLabels_ReturnsNonEmptyLabelForEveryStatus()
    {
        Assert.All(Enum.GetValues<InstrumentRentalStatus>(), status => Assert.False(string.IsNullOrWhiteSpace(ReportLabels.Rental(status))));
        Assert.All(Enum.GetValues<AttendanceStatus>(), status => Assert.False(string.IsNullOrWhiteSpace(ReportLabels.Attendance(status))));
    }

    [Fact]
    public void ReportLabels_Throws_ForStatusOutsideTheEnum()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ReportLabels.Rental((InstrumentRentalStatus)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => ReportLabels.Attendance((AttendanceStatus)99));
    }

    private static async Task<Assignment> AddAssignmentAsync(AcademicHarness harness, string name)
    {
        var assignment = new Assignment(name, "Do it", Now.AddDays(7), harness.Lecture.Id);
        harness.Context.Set<Assignment>().Add(assignment);
        await harness.Context.SaveChangesAsync();
        return assignment;
    }

    private static async Task AddGradeAsync(AcademicHarness harness, Assignment assignment, Student student, int grade)
    {
        var submission = new AssignmentSubmission(assignment.Id, student.Id);
        submission.Submit("/hw.pdf", Now);
        submission.SetGrade(grade);
        harness.Context.Set<AssignmentSubmission>().Add(submission);
        await harness.Context.SaveChangesAsync();
    }

    private static async Task<(MusicStore Store, InstrumentType Type)> AddStoreAsync(ENoteContext context, string name)
    {
        var type = new InstrumentType { Type = "Guitar", MonthlyFee = 50m };
        context.Set<InstrumentType>().Add(type);
        var store = new MusicStore(name, "09-17");
        context.Set<MusicStore>().Add(store);
        await context.SaveChangesAsync();
        return (store, type);
    }

    private static async Task<Instrument> AddInstrumentAsync(ENoteContext context, MusicStore store, InstrumentType type, string model)
    {
        var instrument = new Instrument(model, "Yamaha", null, null, type.Id, store.Id);
        context.Set<Instrument>().Add(instrument);
        await context.SaveChangesAsync();
        return instrument;
    }

    private static ReportService CreateService(AcademicHarness harness, Instructor instructor, IStoreContext? stores = null, IStudentDisplayNameService? names = null)
    {
        var actor = new StubCurrentActor(student: harness.Student);
        var clock = new FixedClock(Now);
        var instructorAccess = AcademicTestData.CreateInstructorAccess(harness.Context, instructor);
        var displayNames = names ?? new StubDisplayNameService();
        var ranking = new RankingService(harness.Context, actor, actor, displayNames, instructorAccess, clock);
        return new ReportService(
            harness.Context,
            clock,
            ranking,
            instructorAccess,
            actor,
            stores ?? new StubCurrentActor(storeId: 1),
            displayNames,
            new PaymentOptions { Currency = "bam" });
    }

    private sealed class NamedDisplayNames(Dictionary<int, string> names) : IStudentDisplayNameService
    {
        public Task<string> GetStudentDisplayNameAsync(Student student, CancellationToken cancellationToken = default) =>
            Task.FromResult(names.GetValueOrDefault(student.Id, $"Student {student.Id}"));

        public Task<IReadOnlyDictionary<int, string>> GetStudentDisplayNamesAsync(IEnumerable<Student> students, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<int, string>>(students.DistinctBy(s => s.Id).ToDictionary(s => s.Id, s => names.GetValueOrDefault(s.Id, $"Student {s.Id}")));
    }

    private sealed class ThrowingStoreContext : IStoreContext
    {
        public Task<int> GetCurrentStoreIdAsync(CancellationToken cancellationToken = default) =>
            throw new StoreNotResolvedException("Store not resolved.");
    }
}
