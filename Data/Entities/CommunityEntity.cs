namespace LocalGovProcessor.Data.Entities;

public class CommunityEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }

    public List<DocumentEntity> Documents { get; set; } = new();
}
