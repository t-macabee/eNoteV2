using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace eNote.Tests.TestUtils;

internal static class PdfText
{
    public static IReadOnlyList<string> Pages(byte[] pdf)
    {
        using var document = PdfDocument.Open(pdf);
        return [.. document.GetPages().Select(page => ContentOrderTextExtractor.GetText(page))];
    }

    public static IReadOnlyList<string> Lines(byte[] pdf) =>
        [.. Pages(pdf).SelectMany(page => page.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))];
}
