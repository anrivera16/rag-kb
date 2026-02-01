namespace KnowledgeBase.API.Models.DTOs;

public class ChatResponse
{
    public string Answer { get; set; } = string.Empty;
    public Guid ConversationId { get; set; }
    public List<SourceReference> Sources { get; set; } = new();
}

public class SourceReference
{
    public Guid DocumentId { get; set; }
    public string Text { get; set; } = string.Empty;
    public double Similarity { get; set; }
}
