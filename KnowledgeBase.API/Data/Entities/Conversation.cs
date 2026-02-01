namespace KnowledgeBase.API.Data.Entities;

public class Conversation
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? Title { get; set; }

    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
