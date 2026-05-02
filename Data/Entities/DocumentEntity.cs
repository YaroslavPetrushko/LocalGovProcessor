namespace LocalGovProcessor.Data.Entities;

public class DocumentEntity
{
    public Guid Id { get; set; }
    public Guid CommunityId { get; set; }
    public short Year { get; set; }
    public string DocType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FileFormat { get; set; } = string.Empty;
    public string Status { get; set; } = "parsed";
    public string? RawText { get; set; }
    public int? ProcessingTimeMs { get; set; }
    public DateTimeOffset UploadedAt { get; set; }

    public CommunityEntity? Community { get; set; }
    public List<SectionEntity> Sections { get; set; } = new();
}
