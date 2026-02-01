namespace KnowledgeBase.API.Data.Entities;

public class Message
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public string Role { get; set; } = string.Empty;  // "user" or "assistant"
    public string Content { get; set; } = string.Empty;
    public string? Sources { get; set; }  // JSONB - stores which chunks were used
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Conversation Conversation { get; set; } = null!;
}
