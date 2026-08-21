using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Application.Features.Academic.Courses.Services;
using eNote.Application.Features.Identity.Instructors;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Application.Features.Rentals.InstrumentRentals;
using eNote.Application.Features.Rentals.InstrumentRentals.Billing;
using eNote.Application.Features.Reports.Services;
using eNote.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;

namespace eNote.Infrastructure.Reports;

public sealed class ReportService(IAppDbContext context, IClock clock, IRankingService rankingService, IInstructorAccessService instructorAccess, ICurrentActor actor, IStudentDisplayNameService displayNames) : IReportService
{
    private static readonly CultureInfo ReportCulture = CultureInfo.GetCultureInfo("bs-BA");

    static ReportService() => QuestPDF.Settings.License = LicenseType.Community;

    public async Task<byte[]> GenerateCourseRankingPdfAsync(int courseId, CancellationToken cancellationToken = default)
    {
        var entries = await rankingService.GetForInstructorAsync(courseId);
        var courseName = await context.Set<Course>().AsNoTracking().Where(c => c.Id == courseId).Select(c => c.Name).FirstOrDefaultAsync(cancellationToken) ?? $"Kurs {courseId}";
        return Document.Create(container => container.Page(page =>
        {
            page.Margin(30);
            page.Header().Text($"Rang lista — {courseName}").Bold().FontSize(18);
            page.Content().PaddingVertical(10).Table(table =>
            {
                table.ColumnsDefinition(columns => { columns.ConstantColumn(40); columns.RelativeColumn(3); columns.RelativeColumn(2); columns.RelativeColumn(2); });
                table.Header(header => { header.Cell().Element(CellStyle).Text("Rang"); header.Cell().Element(CellStyle).Text("Student"); header.Cell().Element(CellStyle).Text("Prosjek"); header.Cell().Element(CellStyle).Text("Ocijenjeno"); });
                foreach (var entry in entries) { table.Cell().Element(CellStyle).Text(entry.Rank.ToString()); table.Cell().Element(CellStyle).Text(entry.StudentName); table.Cell().Element(CellStyle).Text(entry.AverageGrade?.ToString("F2", ReportCulture) ?? "-"); table.Cell().Element(CellStyle).Text(entry.GradedSubmissions.ToString()); }
            });
            page.Footer().AlignRight().Text($"Generisano: {clock.UtcNow:dd.MM.yyyy HH:mm} UTC").FontSize(9);
        })).GeneratePdf();
    }

    public async Task<byte[]> GenerateStoreRentalSummaryPdfAsync(CancellationToken cancellationToken = default)
    {
        var storeId = await actor.GetCurrentStoreIdAsync(cancellationToken);
        var storeName = await context.Set<MusicStore>().AsNoTracking().Where(s => s.Id == storeId).Select(s => s.StoreName).FirstOrDefaultAsync(cancellationToken) ?? $"Prodavnica {storeId}";
        var rentals = await context.Set<InstrumentRental>().AsNoTracking().Include(x => x.Instrument).Include(x => x.StudentProfile).Where(x => x.Instrument.MusicStoreId == storeId).OrderByDescending(x => x.RequestedAt).ToListAsync(cancellationToken);
        return Document.Create(container => container.Page(page =>
        {
            page.Margin(30);
            page.Header().Text($"Pregled iznajmljivanja — {storeName}").Bold().FontSize(18);
            page.Content().PaddingVertical(10).Table(table =>
            {
                table.ColumnsDefinition(columns => { columns.ConstantColumn(35); columns.RelativeColumn(2); columns.RelativeColumn(2); columns.RelativeColumn(2); columns.RelativeColumn(2); });
                table.Header(header => { header.Cell().Element(CellStyle).Text("ID"); header.Cell().Element(CellStyle).Text("Instrument"); header.Cell().Element(CellStyle).Text("Status"); header.Cell().Element(CellStyle).Text("Naknada"); header.Cell().Element(CellStyle).Text("Ukupno"); });
                foreach (var rental in rentals) { var dto = new InstrumentRentalDto { Fee = rental.Fee, RentalStatus = rental.RentalStatus }; RentalBilling.ApplyBilling(rental, dto, clock.UtcNow); table.Cell().Element(CellStyle).Text(rental.Id.ToString()); table.Cell().Element(CellStyle).Text(rental.Instrument.Model); table.Cell().Element(CellStyle).Text(rental.RentalStatus.ToString()); table.Cell().Element(CellStyle).Text(rental.Fee.ToString("F2", ReportCulture)); table.Cell().Element(CellStyle).Text(dto.TotalFee?.ToString("F2", ReportCulture) ?? "-"); }
            });
            page.Footer().AlignRight().Text($"Generisano: {clock.UtcNow:dd.MM.yyyy HH:mm} UTC").FontSize(9);
        })).GeneratePdf();
    }

    public async Task<byte[]> GenerateLectureAttendancePdfAsync(int lectureId, CancellationToken cancellationToken = default)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(actor.UserId);
        var lecture = await instructorAccess.GetOwnedLectureAsync(lectureId, instructorId, includeAttendances: true);
        var nameMap = await displayNames.GetStudentDisplayNamesAsync(lecture.Attendances.Select(a => a.Student));
        var rows = lecture.Attendances.OrderBy(a => a.StudentId).Select(a => new AttendanceRow(nameMap.GetValueOrDefault(a.StudentId, $"Student {a.StudentId}"), a.AttendanceStatus)).ToList();
        return Document.Create(container => container.Page(page =>
        {
            page.Margin(30);
            page.Header().Column(column => { column.Item().Text($"Prisustvo — {lecture.Name}").Bold().FontSize(18); column.Item().Text($"{lecture.LectureTime:dd.MM.yyyy HH:mm} · {lecture.Location}").FontSize(11); });
            page.Content().PaddingVertical(10).Table(table =>
            {
                table.ColumnsDefinition(columns => { columns.RelativeColumn(3); columns.RelativeColumn(2); });
                table.Header(header => { header.Cell().Element(CellStyle).Text("Student"); header.Cell().Element(CellStyle).Text("Status"); });
                foreach (var row in rows) { table.Cell().Element(CellStyle).Text(row.StudentName); table.Cell().Element(CellStyle).Text(row.Status.ToString()); }
            });
            page.Footer().AlignRight().Text($"Generisano: {clock.UtcNow:dd.MM.yyyy HH:mm} UTC").FontSize(9);
        })).GeneratePdf();
    }

    private static IContainer CellStyle(IContainer container) => container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(4).PaddingHorizontal(2);
    private sealed record AttendanceRow(string StudentName, AttendanceStatus Status);
}
