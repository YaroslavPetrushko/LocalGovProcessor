namespace LocalGovProcessor.Data.Entities;

public class SectionEntity
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public short Position { get; set; }
    public short Level { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }

    public DocumentEntity? Document { get; set; }
}
