using eNote.Application.Common.Localization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;

namespace eNote.Infrastructure.Reports;

internal sealed record ReportColumn(string Header, float Weight);

internal sealed record ReportSpec(
    string Title,
    string? Subtitle,
    IReadOnlyList<ReportColumn> Columns,
    IReadOnlyList<string[]> Rows,
    string? Summary);

internal static class ReportRenderer
{
    private const float PageMargin = 30;
    private const float ContentPadding = 10;
    private const float SummarySpacing = 8;
    private const float CellVerticalPadding = 4;
    private const float CellHorizontalPadding = 2;
    private const float TitleSize = 18;
    private const float BodySize = 11;
    private const float FooterSize = 9;
    private const string GeneratedAtFormat = "dd.MM.yyyy HH:mm";

    static ReportRenderer() => QuestPDF.Settings.License = LicenseType.Community;

    public static byte[] Render(ReportSpec spec, DateTime generatedAtUtc) =>
        Document.Create(document => document.Page(page =>
        {
            page.Margin(PageMargin);
            page.Header().Column(column =>
            {
                column.Item().Text(spec.Title).Bold().FontSize(TitleSize);
                if (spec.Subtitle is not null)
                {
                    column.Item().Text(spec.Subtitle).FontSize(BodySize);
                }
            });
            page.Content().PaddingVertical(ContentPadding).Element(content => ComposeContent(content, spec));
            page.Footer().Row(row =>
            {
                row.RelativeItem().Text($"{Messages.ReportGeneratedLabel}: {generatedAtUtc.ToString(GeneratedAtFormat, CultureInfo.InvariantCulture)} UTC").FontSize(FooterSize);
                row.RelativeItem().AlignRight().Text(text =>
                {
                    text.DefaultTextStyle(style => style.FontSize(FooterSize));
                    text.Span($"{Messages.ReportPageLabel} ");
                    text.CurrentPageNumber();
                    text.Span($" {Messages.ReportPageOf} ");
                    text.TotalPages();
                });
            });
        })).GeneratePdf();

    private static void ComposeContent(IContainer content, ReportSpec spec)
    {
        if (spec.Rows.Count == 0)
        {
            content.Text(Messages.ReportNoData).FontSize(BodySize);
            return;
        }

        content.Column(column =>
        {
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    foreach (var definition in spec.Columns)
                    {
                        columns.RelativeColumn(definition.Weight);
                    }
                });
                table.Header(header =>
                {
                    foreach (var definition in spec.Columns)
                    {
                        header.Cell().Element(CellStyle).Text(definition.Header);
                    }
                });
                foreach (var value in spec.Rows.SelectMany(row => row))
                {
                    table.Cell().Element(CellStyle).Text(value);
                }
            });

            if (spec.Summary is not null)
            {
                column.Item().PaddingTop(SummarySpacing).Text(spec.Summary).Bold().FontSize(BodySize);
            }
        });
    }

    private static IContainer CellStyle(IContainer container) =>
        container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(CellVerticalPadding).PaddingHorizontal(CellHorizontalPadding);
}
