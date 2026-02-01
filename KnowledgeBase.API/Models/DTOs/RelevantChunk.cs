namespace KnowledgeBase.API.Models.DTOs;

public class RelevantChunk
{
    public Guid DocumentId { get; set; }
    public string Text { get; set; } = string.Empty;
    public double Similarity { get; set; }
}
