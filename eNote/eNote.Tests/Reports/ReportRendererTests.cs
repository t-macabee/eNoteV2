using eNote.Infrastructure.Reports;
using eNote.Tests.TestUtils;

namespace eNote.Tests.Reports;

public sealed class ReportRendererTests
{
    private static readonly DateTime GeneratedAt = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly ReportColumn[] Columns = [new("Student", 3), new("Status", 2)];

    [Fact]
    public void Render_ShowsTitleSubtitleHeaderRowsSummaryAndFooter_OnOnePage()
    {
        var spec = new ReportSpec("Prisustvo — Test", "01.07.2026 12:00 · Učionica 2", Columns, [["Ana Anić", "Prisutan"], ["Bojan Bošnjak", "Odsutan"]], "Ukupno: 2");

        var lines = PdfText.Lines(ReportRenderer.Render(spec, GeneratedAt));

        Assert.Equal(
        [
            "Prisustvo — Test",
            "01.07.2026 12:00 · Učionica 2",
            "Student Status",
            "Ana Anić Prisutan",
            "Bojan Bošnjak Odsutan",
            "Ukupno: 2",
            "Generisano: 01.07.2026 12:00 UTC Stranica 1 od 1"
        ], lines);
    }

    [Fact]
    public void Render_KeepsBosnianLettersInTitleAndCells()
    {
        var spec = new ReportSpec("ČĆŠŽĐ čćšžđ", null, Columns, [["Đorđe Šaćirović", "Željko Čolić"]], null);

        var lines = PdfText.Lines(ReportRenderer.Render(spec, GeneratedAt));

        Assert.Contains("ČĆŠŽĐ čćšžđ", lines);
        Assert.Contains("Đorđe Šaćirović Željko Čolić", lines);
    }

    [Fact]
    public void Render_ShowsTheNoDataLineAndNoTableOrSummary_WhenThereAreNoRows()
    {
        var spec = new ReportSpec("Rang lista — Kurs", null, Columns, [], "Ukupno: 0");

        var lines = PdfText.Lines(ReportRenderer.Render(spec, GeneratedAt));

        Assert.Equal(
        [
            "Rang lista — Kurs",
            "Nema podataka za odabrane kriterije.",
            "Generisano: 01.07.2026 12:00 UTC Stranica 1 od 1"
        ], lines);
    }

    [Fact]
    public void Render_RepeatsTitleAndHeaderOnEveryPage_NumbersThePages_AndShowsEachRowOnce()
    {
        var rows = Enumerable.Range(1, 120).Select(i => new[] { $"Student broj {i}", "Prisutan" }).ToList();
        var spec = new ReportSpec("Prisustvo — Dugi spisak", null, Columns, rows, "Ukupno: 120");

        var pages = PdfText.Pages(ReportRenderer.Render(spec, GeneratedAt));

        Assert.True(pages.Count > 1);
        for (var index = 0; index < pages.Count; index++)
        {
            var pageLines = pages[index].Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            Assert.Equal("Prisustvo — Dugi spisak", pageLines[0]);
            Assert.Equal("Student Status", pageLines[1]);
            Assert.Equal($"Generisano: 01.07.2026 12:00 UTC Stranica {index + 1} od {pages.Count}", pageLines[^1]);
        }

        var allLines = pages.SelectMany(page => page.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)).ToList();
        Assert.All(rows, row => Assert.Single(allLines, line => line == $"{row[0]} {row[1]}"));
        Assert.Single(allLines, line => line == "Ukupno: 120");
    }
}
