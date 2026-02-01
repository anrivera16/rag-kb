namespace KnowledgeBase.API.Models.DTOs;

public class AskRequest
{
    public string Question { get; set; } = string.Empty;
    public Guid? ConversationId { get; set; }
}
