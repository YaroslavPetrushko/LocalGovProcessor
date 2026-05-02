using LocalGovProcessor.Models;
using UglyToad.PdfPig;

namespace LocalGovProcessor.Services;

public class PdfParserService
{
    public List<DocumentSection> Parse(Stream fileStream)
    {
        var sections = new List<DocumentSection>();

        using var pdf = PdfDocument.Open(fileStream);

        foreach (var page in pdf.GetPages())
        {
            var text = page.Text?.Trim();
            if (string.IsNullOrWhiteSpace(text))
                continue;

            sections.Add(new DocumentSection
            {
                Level = 0,
                Title = $"Page {page.Number}",
                Content = NormalizeWhitespace(text)
            });
        }

        return sections;
    }

    private static string NormalizeWhitespace(string text)
    {
        return string.Join(" ", text
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }
}
