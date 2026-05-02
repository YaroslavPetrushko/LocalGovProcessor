using LocalGovProcessor.Models;
using LocalGovProcessor.Services;
using Microsoft.AspNetCore.Mvc;

namespace LocalGovProcessor.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    private readonly DocxParserService _docxParser;
    private readonly PdfParserService _pdfParser;

    public UploadController(DocxParserService docxParser, PdfParserService pdfParser)
    {
        _docxParser = docxParser;
        _pdfParser = pdfParser;
    }

    // Accepts multipart/form-data: one .docx/.pdf file + community metadata fields
    [HttpPost]
    public IActionResult Upload(
        IFormFile file,
        [FromForm] string communityName,
        [FromForm] string region,
        [FromForm] int year,
        [FromForm] string docType)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Файл відсутній.");

        var extension = Path.GetExtension(file.FileName);
        if (!IsSupportedExtension(extension))
            return BadRequest("Приймаються лише .docx та .pdf файли.");

        if (file.Length > 20 * 1024 * 1024)
            return BadRequest("Файл перевищує 20 MB.");
        
        // Track how long parsing takes — surfaced in metadata for observability
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var sections = ParseSections(file, extension);
        stopwatch.Stop(); 

        if (sections.Count == 0)
            return UnprocessableEntity("Документ не містить тексту або має непідтримувану структуру.");

        var result = new DocumentResult
        {
            CommunityName = communityName,
            Region = region,
            Year = year,
            DocType = docType,
            Metadata = new DocumentMetadata
            {
                TotalSections = sections.Count,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                
                // Group sections by heading level to give a quick structural overview
                SectionsByLevel = sections
                    .Where(s => s.Level > 0)
                    .GroupBy(s => s.Level.ToString())
                    .ToDictionary(g => g.Key, g => g.Count())
            },
            Sections = sections
        };

        return Ok(result);
    }

    private List<DocumentSection> ParseSections(IFormFile file, string? extension)
    {
        using var stream = file.OpenReadStream();

        return extension?.ToLowerInvariant() switch
        {
            ".docx" => _docxParser.Parse(stream),
            ".pdf" => _pdfParser.Parse(stream),
            _ => new List<DocumentSection>()
        };
    }

    private static bool IsSupportedExtension(string? extension)
    {
        return extension?.ToLowerInvariant() is ".docx" or ".pdf";
    }
}
