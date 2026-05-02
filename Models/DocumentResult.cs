namespace LocalGovProcessor.Models;

// Root response object returned by the API after processing a document
public class DocumentResult
{
    public string CommunityName { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public int Year { get; set; }
    public string DocType { get; set; } = string.Empty;
    public DocumentMetadata Metadata { get; set; } = new();
    public List<DocumentSection> Sections { get; set; } = new();
}

// Summary statistics computed after parsing, before any LLM processing
public class DocumentMetadata
{
    public string Status { get; set; } = "parsed";
    public int TotalSections { get; set; }
    
    // Number of headings per level, e.g. { "1": 2, "2": 3 }
    public Dictionary<string, int> SectionsByLevel { get; set; } = new();
    public long ProcessingTimeMs { get; set; }
}

public class DocumentSection
{
    // Level 1 = Heading1, Level 2 = Heading2, Level 3 = Heading3, Level 0 = unsectioned text
    public int Level { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}