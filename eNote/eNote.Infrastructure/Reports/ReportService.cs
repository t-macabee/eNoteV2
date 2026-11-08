using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Application.Features.Academic.Courses.Services;
using eNote.Application.Features.Academic.Lectures;
using eNote.Application.Features.Identity.Instructors;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Application.Features.Payments;
using eNote.Application.Features.Reports.Services;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace eNote.Infrastructure.Reports;

internal sealed class ReportService(
    IAppDbContext context,
    IClock clock,
    RankingService rankingService,
    InstructorAccessService instructorAccess,
    ICurrentUserContext currentUser,
    IStoreContext stores,
    IStudentDisplayNameService displayNames,
    PaymentOptions payment) : IReportService
{
    private const string Dash = "-";
    private const string AmountFormat = "F2";
    private const string DateFormat = "dd.MM.yyyy";
    private const string DateTimeFormat = "dd.MM.yyyy HH:mm";

    private static readonly CultureInfo ReportCulture = CultureInfo.GetCultureInfo("bs-BA");
    private static readonly StringComparer NameComparer = StringComparer.Create(ReportCulture, ignoreCase: false);

    public async Task<byte[]> GenerateCourseRankingPdfAsync(int courseId, CancellationToken cancellationToken = default)
    {
        var entries = await rankingService.GetFullForInstructorAsync(courseId, cancellationToken);
        var courseName = await context.Set<Course>().AsNoTracking().Where(c => c.Id == courseId).Select(c => c.Name).FirstOrDefaultAsync(cancellationToken) ?? Messages.ReportCourseFallback;

        return ReportRenderer.Render(new ReportSpec(
            $"{Messages.ReportRankingTitle} — {courseName}",
            null,
            [
                new(Messages.ReportColumnRank, 1),
                new(Messages.ReportColumnStudent, 4),
                new(Messages.ReportColumnAverage, 2),
                new(Messages.ReportColumnGraded, 2)
            ],
            [.. entries.Select(e => new[] { e.Rank.ToString(ReportCulture), e.StudentName, e.AverageGrade?.ToString(AmountFormat, ReportCulture) ?? Dash, e.GradedSubmissions.ToString(ReportCulture) })],
            Messages.ReportSummaryRanking(entries.Count)), clock.UtcNow);
    }

    public async Task<byte[]> GenerateStoreRentalSummaryPdfAsync(CancellationToken cancellationToken = default)
    {
        var storeId = await stores.GetCurrentStoreIdAsync(cancellationToken);
        var storeName = await context.Set<MusicStore>().AsNoTracking().Where(s => s.Id == storeId).Select(s => s.StoreName).FirstOrDefaultAsync(cancellationToken) ?? Messages.ReportStoreFallback;
        var rentals = await LoadRentalSummaryRowsAsync(storeId, cancellationToken);
        var studentNames = await displayNames.GetStudentDisplayNamesAsync(rentals.Select(r => r.Student).DistinctBy(s => s.Id), cancellationToken);
        var now = clock.UtcNow;
        var currency = payment.Currency.ToUpperInvariant();
        var lines = rentals.Select(rental => (Rental: rental, Charges: rental.CalculateCharges(now))).ToList();

        return ReportRenderer.Render(new ReportSpec(
            $"{Messages.ReportRentalSummaryTitle} — {storeName}",
            Messages.ReportCurrencyNote(currency),
            [
                new(Messages.ReportColumnInstrument, 3),
                new(Messages.ReportColumnStudent, 3),
                new(Messages.ReportColumnStatus, 3),
                new(Messages.ReportColumnPeriod, 6),
                new(Messages.ReportColumnFee, 2),
                new(Messages.ReportColumnPenalty, 2),
                new(Messages.ReportColumnTotal, 2)
            ],
            [.. lines.Select(line => new[]
            {
                line.Rental.InstrumentModel,
                studentNames.GetValueOrDefault(line.Rental.Student.Id, Messages.UnknownUserName),
                ReportLabels.Rental(line.Rental.RentalStatus),
                FormatPeriod(line.Rental.PickedUpAt, line.Rental.ReturnedAt),
                FormatAmount(line.Rental.ApprovedAt.HasValue ? line.Rental.Fee : null),
                FormatAmount(line.Charges.PenaltyFee),
                FormatAmount(line.Charges.TotalFee)
            })],
            Messages.ReportSummaryRentals(lines.Count, FormatAmount(lines.Sum(line => line.Charges.TotalFee ?? 0m)), currency)), now);
    }

    public async Task<byte[]> GenerateLectureAttendancePdfAsync(int lectureId, CancellationToken cancellationToken = default)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUser.UserId, cancellationToken);
        var lecture = await instructorAccess.GetOwnedLectureAsync(lectureId, instructorId, cancellationToken: cancellationToken);
        var attendances = await LoadAttendanceRowsAsync(lecture.Id, cancellationToken);

        return ReportRenderer.Render(new ReportSpec(
            $"{Messages.ReportAttendanceTitle} — {lecture.Name}",
            $"{lecture.LectureTime.ToString(DateTimeFormat, ReportCulture)} · {lecture.Location}",
            [
                new(Messages.ReportColumnStudent, 3),
                new(Messages.ReportColumnStatus, 2)
            ],
            [.. attendances.Select(row => new[] { row.StudentName, ReportLabels.Attendance(row.Status) })],
            Messages.ReportSummaryAttendance(
                attendances.Count(row => row.Status == AttendanceStatus.Present),
                attendances.Count(row => row.Status == AttendanceStatus.Absent),
                attendances.Count(row => row.Status == AttendanceStatus.Pending))), clock.UtcNow);
    }

    public async Task<byte[]> GenerateAdminMusicStoreReportAsync(CancellationToken cancellationToken = default)
    {
        var musicStores = await context.Set<MusicStore>()
            .AsNoTracking()
            .OrderBy(s => s.StoreName)
            .ThenBy(s => s.Id)
            .Select(s => new MusicStoreRow(
                s.StoreName,
                s.Address == null ? null : s.Address.City.Name,
                s.Address == null ? null : s.Address.Street + " " + s.Address.Number,
                s.BusinessHours))
            .ToListAsync(cancellationToken);

        return ReportRenderer.Render(new ReportSpec(
            Messages.ReportStoreReportTitle,
            null,
            [
                new(Messages.ReportColumnName, 3),
                new(Messages.ReportColumnCity, 2),
                new(Messages.ReportColumnAddress, 3),
                new(Messages.ReportColumnBusinessHours, 2)
            ],
            [.. musicStores.Select(store => new[] { store.StoreName, store.City ?? Dash, store.Address ?? Dash, store.BusinessHours })],
            Messages.ReportSummaryStores(musicStores.Count)), clock.UtcNow);
    }

    private static string FormatAmount(decimal? amount) => amount?.ToString(AmountFormat, ReportCulture) ?? Dash;

    private static string FormatDate(DateTime date) => date.ToString(DateFormat, ReportCulture);

    private static string FormatPeriod(DateTime? pickedUpAt, DateTime? returnedAt) => (pickedUpAt, returnedAt) switch
    {
        (null, _) => Dash,
        ({ } start, null) => $"{FormatDate(start)} –",
        ({ } start, { } end) => $"{FormatDate(start)} – {FormatDate(end)}"
    };

    internal async Task<List<AttendanceRow>> LoadAttendanceRowsAsync(int lectureId, CancellationToken cancellationToken)
    {
        var attended = await context.Set<Attendance>()
            .AsNoTracking()
            .Include(a => a.Student)
            .Where(a => a.LectureId == lectureId)
            .ShownForLecture()
            .ToListAsync(cancellationToken);

        var nameMap = await displayNames.GetStudentDisplayNamesAsync(attended.Select(a => a.Student), cancellationToken);

        return [.. attended
            .Select(a => new AttendanceRow(a.StudentId, nameMap.GetValueOrDefault(a.StudentId, Messages.UnknownUserName), a.AttendanceStatus))
            .OrderBy(row => row.StudentName, NameComparer)
            .ThenBy(row => row.StudentId)];
    }

    internal sealed record AttendanceRow(int StudentId, string StudentName, AttendanceStatus Status);

    internal sealed record MusicStoreRow(string StoreName, string? City, string? Address, string BusinessHours);

    internal async Task<List<RentalSummaryRow>> LoadRentalSummaryRowsAsync(int storeId, CancellationToken cancellationToken) =>
        await context.Set<InstrumentRental>()
            .AsNoTracking()
            .Where(x => x.MusicStoreId == storeId)
            .OrderByDescending(x => x.RequestedAt)
            .ThenByDescending(x => x.Id)
            .Select(x => new RentalSummaryRow
            {
                InstrumentModel = x.Instrument.Model,
                Student = x.StudentProfile,
                RentalStatus = x.RentalStatus,
                Fee = x.Fee,
                ApprovedAt = x.ApprovedAt,
                PickedUpAt = x.PickedUpAt,
                ReturnedAt = x.ReturnedAt,
                DueAt = x.DueAt,
                OverduePenaltyMultiplier = x.OverduePenaltyMultiplier
            })
            .ToListAsync(cancellationToken);

    internal sealed record RentalSummaryRow
    {
        public string InstrumentModel { get; init; } = string.Empty;
        public Student Student { get; init; } = null!;
        public InstrumentRentalStatus RentalStatus { get; init; }
        public decimal Fee { get; init; }
        public DateTime? ApprovedAt { get; init; }
        public DateTime? PickedUpAt { get; init; }
        public DateTime? ReturnedAt { get; init; }
        public DateTime? DueAt { get; init; }
        public decimal OverduePenaltyMultiplier { get; init; }

        public RentalCharges CalculateCharges(DateTime now) =>
            RentalChargeCalculator.Calculate(PickedUpAt, ReturnedAt, DueAt, RentalStatus, Fee, OverduePenaltyMultiplier, now);
    }
}
