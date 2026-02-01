using Pgvector;

namespace KnowledgeBase.API.Data.Entities;

public class DocumentChunk
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public string ChunkText { get; set; } = string.Empty;
    public int ChunkIndex { get; set; }
    public Vector? Embedding { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? Metadata { get; set; }

    public Document Document { get; set; } = null!;
}
