using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Application.Features.Academic.Courses.Services;
using eNote.Application.Features.Identity.Instructors;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Application.Features.Reports.Services;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;

namespace eNote.Infrastructure.Reports;

internal sealed class ReportService(IAppDbContext context, IClock clock, RankingService rankingService, InstructorAccessService instructorAccess, ICurrentUserContext currentUser, IStoreContext stores, IStudentDisplayNameService displayNames) : IReportService
{
    private static readonly CultureInfo ReportCulture = CultureInfo.GetCultureInfo("bs-BA");

    static ReportService() => QuestPDF.Settings.License = LicenseType.Community;

    public async Task<byte[]> GenerateCourseRankingPdfAsync(int courseId, CancellationToken cancellationToken = default)
    {
        var entries = await rankingService.GetForInstructorAsync(courseId, cancellationToken);
        var courseName = await context.Set<Course>().AsNoTracking().Where(c => c.Id == courseId).Select(c => c.Name).FirstOrDefaultAsync(cancellationToken) ?? $"{Messages.ReportCourseFallback} {courseId}";
        return Document.Create(container => container.Page(page =>
        {
            page.Margin(30);
            page.Header().Text($"{Messages.ReportRankingTitle} — {courseName}").Bold().FontSize(18);
            page.Content().PaddingVertical(10).Table(table =>
            {
                table.ColumnsDefinition(columns => { columns.ConstantColumn(40); columns.RelativeColumn(3); columns.RelativeColumn(2); columns.RelativeColumn(2); });
                table.Header(header => { header.Cell().Element(CellStyle).Text(Messages.ReportColumnRank); header.Cell().Element(CellStyle).Text(Messages.ReportColumnStudent); header.Cell().Element(CellStyle).Text(Messages.ReportColumnAverage); header.Cell().Element(CellStyle).Text(Messages.ReportColumnGraded); });
                foreach (var entry in entries) { table.Cell().Element(CellStyle).Text(entry.Rank.ToString()); table.Cell().Element(CellStyle).Text(entry.StudentName); table.Cell().Element(CellStyle).Text(entry.AverageGrade?.ToString("F2", ReportCulture) ?? "-"); table.Cell().Element(CellStyle).Text(entry.GradedSubmissions.ToString()); }
            });
            page.Footer().AlignRight().Text($"{Messages.ReportGeneratedLabel}: {clock.UtcNow:dd.MM.yyyy HH:mm} UTC").FontSize(9);
        })).GeneratePdf();
    }

    public async Task<byte[]> GenerateStoreRentalSummaryPdfAsync(CancellationToken cancellationToken = default)
    {
        var storeId = await stores.GetCurrentStoreIdAsync(cancellationToken);
        var storeName = await context.Set<MusicStore>().AsNoTracking().Where(s => s.Id == storeId).Select(s => s.StoreName).FirstOrDefaultAsync(cancellationToken) ?? $"{Messages.ReportStoreFallback} {storeId}";
        var rentals = await context.Set<InstrumentRental>()
            .AsNoTracking()
            .Where(x => x.MusicStoreId == storeId)
            .OrderByDescending(x => x.RequestedAt)
            .Select(x => new RentalSummaryRow
            {
                Id = x.Id,
                InstrumentModel = x.Instrument.Model,
                RentalStatus = x.RentalStatus,
                Fee = x.Fee,
                PickedUpAt = x.PickedUpAt,
                ReturnedAt = x.ReturnedAt
            })
            .ToListAsync(cancellationToken);
        return Document.Create(container => container.Page(page =>
        {
            page.Margin(30);
            page.Header().Text($"{Messages.ReportRentalSummaryTitle} — {storeName}").Bold().FontSize(18);
            page.Content().PaddingVertical(10).Table(table =>
            {
                table.ColumnsDefinition(columns => { columns.ConstantColumn(35); columns.RelativeColumn(2); columns.RelativeColumn(2); columns.RelativeColumn(2); columns.RelativeColumn(2); });
                table.Header(header => { header.Cell().Element(CellStyle).Text(Messages.ReportColumnId); header.Cell().Element(CellStyle).Text(Messages.ReportColumnInstrument); header.Cell().Element(CellStyle).Text(Messages.ReportColumnStatus); header.Cell().Element(CellStyle).Text(Messages.ReportColumnFee); header.Cell().Element(CellStyle).Text(Messages.ReportColumnTotal); });
                foreach (var rental in rentals) { var charges = rental.CalculateCharges(clock.UtcNow); table.Cell().Element(CellStyle).Text(rental.Id.ToString()); table.Cell().Element(CellStyle).Text(rental.InstrumentModel); table.Cell().Element(CellStyle).Text(rental.RentalStatus.ToString()); table.Cell().Element(CellStyle).Text(rental.Fee.ToString("F2", ReportCulture)); table.Cell().Element(CellStyle).Text(charges.TotalFee?.ToString("F2", ReportCulture) ?? "-"); }
            });
            page.Footer().AlignRight().Text($"{Messages.ReportGeneratedLabel}: {clock.UtcNow:dd.MM.yyyy HH:mm} UTC").FontSize(9);
        })).GeneratePdf();
    }

    public async Task<byte[]> GenerateLectureAttendancePdfAsync(int lectureId, CancellationToken cancellationToken = default)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUser.UserId);
        var lecture = await instructorAccess.GetOwnedLectureAsync(lectureId, instructorId, includeAttendances: true, cancellationToken: cancellationToken);
        var nameMap = await displayNames.GetStudentDisplayNamesAsync(lecture.Attendances.Select(a => a.Student!), cancellationToken);
        var rows = lecture.Attendances.OrderBy(a => a.StudentId).Select(a => new AttendanceRow(nameMap.GetValueOrDefault(a.StudentId, $"{Messages.ReportStudentFallback} {a.StudentId}"), a.AttendanceStatus)).ToList();
        return Document.Create(container => container.Page(page =>
        {
            page.Margin(30);
            page.Header().Column(column => { column.Item().Text($"{Messages.ReportAttendanceTitle} — {lecture.Name}").Bold().FontSize(18); column.Item().Text($"{lecture.LectureTime:dd.MM.yyyy HH:mm} · {lecture.Location}").FontSize(11); });
            page.Content().PaddingVertical(10).Table(table =>
            {
                table.ColumnsDefinition(columns => { columns.RelativeColumn(3); columns.RelativeColumn(2); });
                table.Header(header => { header.Cell().Element(CellStyle).Text(Messages.ReportColumnStudent); header.Cell().Element(CellStyle).Text(Messages.ReportColumnStatus); });
                foreach (var row in rows) { table.Cell().Element(CellStyle).Text(row.StudentName); table.Cell().Element(CellStyle).Text(row.Status.ToString()); }
            });
            page.Footer().AlignRight().Text($"{Messages.ReportGeneratedLabel}: {clock.UtcNow:dd.MM.yyyy HH:mm} UTC").FontSize(9);
        })).GeneratePdf();
    }

    public async Task<byte[]> GenerateAdminMusicStoreReportAsync(CancellationToken cancellationToken = default)
    {
        var stores = await context.Set<MusicStore>().AsNoTracking().OrderBy(s => s.StoreName).ToListAsync(cancellationToken);
        return Document.Create(container => container.Page(page =>
        {
            page.Margin(30);
            page.Header().Text(Messages.ReportStoreReportTitle).Bold().FontSize(18);
            page.Content().PaddingVertical(10).Table(table =>
            {
                table.ColumnsDefinition(columns => { columns.ConstantColumn(35); columns.RelativeColumn(3); columns.RelativeColumn(2); });
                table.Header(header => { header.Cell().Element(CellStyle).Text(Messages.ReportColumnId); header.Cell().Element(CellStyle).Text(Messages.ReportColumnName); header.Cell().Element(CellStyle).Text(Messages.ReportColumnBusinessHours); });
                foreach (var store in stores) { table.Cell().Element(CellStyle).Text(store.Id.ToString()); table.Cell().Element(CellStyle).Text(store.StoreName); table.Cell().Element(CellStyle).Text(store.BusinessHours); }
            });
            page.Footer().AlignRight().Text($"{Messages.ReportGeneratedLabel}: {clock.UtcNow:dd.MM.yyyy HH:mm} UTC").FontSize(9);
        })).GeneratePdf();
    }

    private static IContainer CellStyle(IContainer container) => container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(4).PaddingHorizontal(2);
    private sealed record AttendanceRow(string StudentName, AttendanceStatus Status);

    private sealed record RentalSummaryRow
    {
        public int Id { get; init; }
        public string InstrumentModel { get; init; } = string.Empty;
        public InstrumentRentalStatus RentalStatus { get; init; }
        public decimal Fee { get; init; }
        public DateTime? PickedUpAt { get; init; }
        public DateTime? ReturnedAt { get; init; }

        public RentalCharges CalculateCharges(DateTime now) =>
            RentalChargeCalculator.Calculate(PickedUpAt, ReturnedAt, RentalStatus, Fee, now);
    }
}
