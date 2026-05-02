using LocalGovProcessor.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LocalGovProcessor.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommunitiesController : ControllerBase
{
    private readonly LocalGovDbContext _db;

    public CommunitiesController(LocalGovDbContext db)
    {
        _db = db;
    }

    // Returns all communities with their document summaries (no sections — lightweight list)
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var communities = await _db.Communities
            .Include(c => c.Documents)
            .OrderBy(c => c.Region)
            .ThenBy(c => c.Name)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Region,
                Documents = c.Documents
                    .OrderByDescending(d => d.Year)
                    .Select(d => new
                    {
                        d.Id,
                        d.Year,
                        d.DocType,
                        d.FileName,
                        d.FileFormat,
                        d.Status,
                        d.ProcessingTimeMs,
                        d.UploadedAt
                    })
            })
            .ToListAsync(cancellationToken);

        return Ok(communities);
    }

    // Returns a single document with all its sections — used by Browse and Compare tabs
    [HttpGet("documents/{documentId:guid}")]
    public async Task<IActionResult> GetDocument(Guid documentId, CancellationToken cancellationToken)
    {
        var document = await _db.Documents
            .Include(d => d.Community)
            .Include(d => d.Sections.OrderBy(s => s.Position))
            .Where(d => d.Id == documentId)
            .Select(d => new
            {
                d.Id,
                CommunityName = d.Community!.Name,
                Region = d.Community.Region,
                d.Year,
                d.DocType,
                d.FileName,
                d.FileFormat,
                d.Status,
                d.ProcessingTimeMs,
                d.UploadedAt,
                Metadata = new
                {
                    Status = d.Status,
                    TotalSections = d.Sections.Count,
                    ProcessingTimeMs = d.ProcessingTimeMs ?? 0,
                    // Sections by level aggregated in memory — EF can't group + dict in one query
                },
                Sections = d.Sections
                    .OrderBy(s => s.Position)
                    .Select(s => new
                    {
                        s.Level,
                        Title = s.Title ?? string.Empty,
                        Content = s.Content ?? string.Empty
                    })
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (document == null)
            return NotFound($"Документ {documentId} не знайдено.");

        return Ok(document);
    }
}