namespace LocalGovProcessor.Models;

public class DocumentResult
{
    public string CommunityName { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public int Year { get; set; }
    public string DocType { get; set; } = string.Empty;
    public List<DocumentSection> Sections { get; set; } = new();
}

public class DocumentSection
{
    public int Level { get; set; }      // 1 = H1, 2 = H2, 3 = звичайний текст
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}