using LocalGovProcessor.Models;
using LocalGovProcessor.Services;
using Microsoft.AspNetCore.Mvc;

namespace LocalGovProcessor.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    private readonly DocxParserService _parser;

    public UploadController(DocxParserService parser)
    {
        _parser = parser;
    }

    // Accepts multipart/form-data: one .docx file + community metadata fields
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

        // Reject anything that isn't a .docx (PDF and other formats are a separate pipeline)
        if (!file.FileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Приймаються лише .docx файли.");

        if (file.Length > 20 * 1024 * 1024)
            return BadRequest("Файл перевищує 20 MB.");
        
        // Track how long parsing takes — surfaced in metadata for observability
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var sections = _parser.Parse(file.OpenReadStream());
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
}