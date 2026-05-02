using LocalGovProcessor.Data;
using LocalGovProcessor.Data.Entities;
using LocalGovProcessor.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LocalGovProcessor.Services;

public class DocumentPersistenceService
{
    private readonly LocalGovDbContext _dbContext;

    public DocumentPersistenceService(LocalGovDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SaveParsedDocumentAsync(
        string fileName,
        string extension,
        string communityName,
        string region,
        int year,
        string docType,
        long processingTimeMs,
        IReadOnlyList<DocumentSection> sections,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var normalizedExtension = NormalizeExtension(extension);
            var normalizedCommunityName = communityName.Trim();
            var normalizedRegion = region.Trim();
            var normalizedDocType = docType.Trim();

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            var community = await _dbContext.Communities
                .SingleOrDefaultAsync(
                    x => x.Name == normalizedCommunityName && x.Region == normalizedRegion,
                    cancellationToken);

            if (community == null)
            {
                community = new CommunityEntity
                {
                    Name = normalizedCommunityName,
                    Region = normalizedRegion
                };

                _dbContext.Communities.Add(community);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            var document = await _dbContext.Documents
                .Include(x => x.Sections)
                .SingleOrDefaultAsync(
                    x => x.CommunityId == community.Id && x.Year == year && x.DocType == normalizedDocType,
                    cancellationToken);

            if (document == null)
            {
                document = new DocumentEntity
                {
                    CommunityId = community.Id,
                    Year = checked((short)year),
                    DocType = normalizedDocType
                };

                _dbContext.Documents.Add(document);
            }
            else if (document.Sections.Count > 0)
            {
                _dbContext.Sections.RemoveRange(document.Sections);
            }

            document.FileName = Path.GetFileName(fileName);
            document.FileFormat = normalizedExtension;
            document.Status = "parsed";
            document.RawText = BuildRawText(sections);
            document.ProcessingTimeMs = ClampProcessingTime(processingTimeMs);
            document.UploadedAt = DateTimeOffset.UtcNow;
            document.Sections = sections
                .Select((section, index) => new SectionEntity
                {
                    Position = checked((short)(index + 1)),
                    Level = checked((short)section.Level),
                    Title = string.IsNullOrWhiteSpace(section.Title) ? null : section.Title.Trim(),
                    Content = string.IsNullOrWhiteSpace(section.Content) ? null : section.Content.Trim()
                })
                .ToList();

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.InvalidPassword)
        {
            throw new DatabaseConnectionException(
                "PostgreSQL відхилив логін або пароль. Перевірте ConnectionStrings:LocalGovProcessor у appsettings.Development.json.",
                ex);
        }
        catch (NpgsqlException ex)
        {
            throw new DatabaseConnectionException(
                "Не вдалося підключитися до PostgreSQL. Перевірте, що сервер запущений і параметри підключення правильні.",
                ex);
        }
    }

    private static string NormalizeExtension(string extension)
    {
        return extension.Trim().TrimStart('.').ToLowerInvariant();
    }

    private static string BuildRawText(IEnumerable<DocumentSection> sections)
    {
        return string.Join(
            Environment.NewLine + Environment.NewLine,
            sections.Select(section =>
            {
                var title = string.IsNullOrWhiteSpace(section.Title) ? null : section.Title.Trim();
                var content = string.IsNullOrWhiteSpace(section.Content) ? null : section.Content.Trim();

                return string.Join(
                    Environment.NewLine,
                    new[] { title, content }.Where(static part => !string.IsNullOrWhiteSpace(part)));
            }).Where(static sectionText => !string.IsNullOrWhiteSpace(sectionText)));
    }

    private static int ClampProcessingTime(long processingTimeMs)
    {
        return processingTimeMs > int.MaxValue ? int.MaxValue : (int)processingTimeMs;
    }
}
