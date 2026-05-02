using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using LocalGovProcessor.Models;

namespace LocalGovProcessor.Services;

public class DocxParserService
{
    public List<DocumentSection> Parse(Stream fileStream)
    {
        var sections = new List<DocumentSection>();

        using var doc = WordprocessingDocument.Open(fileStream, false);
        var mainPart = doc.MainDocumentPart;
        var body = mainPart?.Document?.Body;

        if (body == null)
            return sections;

        // Accumulates text under the current heading until the next heading appears
        DocumentSection? currentSection = null;

        foreach (var paragraph in body.Elements<Paragraph>())
        {
            var text = paragraph.InnerText.Trim();

            if (string.IsNullOrWhiteSpace(text))
                continue;

            // Extract level using the new helper method
            int level = GetHeadingLevel(paragraph, mainPart);

            if (level > 0)
            {
                currentSection = new DocumentSection
                {
                    Level = level,
                    Title = text,
                    Content = string.Empty
                };
                sections.Add(currentSection);
            }
            else
            {
                if (currentSection != null)
                {
                    currentSection.Content += (currentSection.Content.Length > 0 ? " " : "") + text;
                }
                else
                {
                    // Text with no parent heading — stored as level 0 (preamble or unstructured content)
                    sections.Add(new DocumentSection { Level = 0, Title = string.Empty, Content = text });
                }
            }
        }

        return sections;
    }

    private int GetHeadingLevel(Paragraph paragraph, MainDocumentPart? mainPart)
    {
        // Strategy 1: Outline level is the most reliable signal.
        // Word stores levels as 0-based (0 = H1), value 9 means "body text" — skip it.
        var outlineLevel = paragraph.ParagraphProperties?.OutlineLevel?.Val?.Value;
        if (outlineLevel.HasValue && outlineLevel.Value < 9)
        {
            return outlineLevel.Value + 1;
        }

        var styleId = paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
        if (string.IsNullOrEmpty(styleId)) return 0;

        // Strategy 2: StyleId like "Heading1", "Heading2" — works for English Word templates
        if (styleId.StartsWith("Heading", StringComparison.OrdinalIgnoreCase))
        {
            string levelString = styleId.Substring(7);
            if (int.TryParse(levelString, out int idLevel))
            {
                return idLevel;
            }
        }

        // Strategy 3: Resolve style by name from the styles registry.
        // Handles non-English Word (e.g. Ukrainian "Заголовок 1") or custom style IDs.
        var stylesPart = mainPart?.StyleDefinitionsPart;
        if (stylesPart?.Styles != null)
        {
            var style = stylesPart.Styles.Elements<Style>().FirstOrDefault(s => s.StyleId == styleId);
            var styleName = style?.StyleName?.Val?.Value;

            if (!string.IsNullOrEmpty(styleName) && 
                styleName.StartsWith("heading ", StringComparison.OrdinalIgnoreCase))
            {
                string levelString = styleName.Substring(8);
                if (int.TryParse(levelString, out int nameLevel))
                {
                    return nameLevel;
                }
            }
        }

        return 0; // Not a heading
    }
}
