using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KnowledgeBase.API.Data;
using KnowledgeBase.API.Data.Entities;

namespace KnowledgeBase.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConversationsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ConversationsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<ActionResult<Conversation>> Create([FromBody] CreateConversationRequest request)
    {
        var conversation = new Conversation
        {
            Title = request.Title
        };

        _context.Conversations.Add(conversation);
        await _context.SaveChangesAsync();

        return Ok(conversation);
    }

    [HttpGet]
    public async Task<ActionResult<List<ConversationDto>>> GetAll()
    {
        var conversations = await _context.Conversations
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new ConversationDto
            {
                Id = c.Id,
                Title = c.Title,
                CreatedAt = c.CreatedAt,
                MessageCount = c.Messages.Count
            })
            .ToListAsync();

        return Ok(conversations);
    }

    [HttpGet("{id}/messages")]
    public async Task<ActionResult<List<MessageDto>>> GetMessages(Guid id)
    {
        var conversation = await _context.Conversations
            .Include(c => c.Messages.OrderBy(m => m.CreatedAt))
            .FirstOrDefaultAsync(c => c.Id == id);

        if (conversation == null)
            return NotFound();

        var messages = conversation.Messages.Select(m => new MessageDto
        {
            Id = m.Id,
            Role = m.Role,
            Content = m.Content,
            Sources = m.Sources,
            CreatedAt = m.CreatedAt
        }).ToList();

        return Ok(messages);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var conversation = await _context.Conversations.FindAsync(id);

        if (conversation == null)
            return NotFound();

        _context.Conversations.Remove(conversation);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

public class CreateConversationRequest
{
    public string? Title { get; set; }
}

public class ConversationDto
{
    public Guid Id { get; set; }
    public string? Title { get; set; }
    public DateTime CreatedAt { get; set; }
    public int MessageCount { get; set; }
}

public class MessageDto
{
    public Guid Id { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Sources { get; set; }
    public DateTime CreatedAt { get; set; }
}
