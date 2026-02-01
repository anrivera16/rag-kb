namespace KnowledgeBase.API.Models.DTOs;

public class DocumentDto
{
    public Guid Id { get; set; }
    public string Filename { get; set; } = string.Empty;
    public string? FileType { get; set; }
    public DateTime UploadedAt { get; set; }
    public bool Processed { get; set; }
    public int ChunkCount { get; set; }
}
