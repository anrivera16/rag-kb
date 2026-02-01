namespace KnowledgeBase.API.Data.Entities;

public class Document
{
    public Guid Id { get; set; }
    public string Filename { get; set; } = string.Empty;
    public string? FileType { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public bool Processed { get; set; } = false;
    public string? Metadata { get; set; }

    public ICollection<DocumentChunk> Chunks { get; set; } = new List<DocumentChunk>();
}