using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KnowledgeBase.API.Data;
using KnowledgeBase.API.Data.Entities;
using KnowledgeBase.API.Models.DTOs;
using KnowledgeBase.API.Services.Interfaces;

namespace KnowledgeBase.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IVectorSearchService _vectorSearchService;
    private readonly IClaudeService _claudeService;

    public ChatController(
        AppDbContext context,
        IVectorSearchService vectorSearchService,
        IClaudeService claudeService)
    {
        _context = context;
        _vectorSearchService = vectorSearchService;
        _claudeService = claudeService;
    }

    [HttpPost("ask")]
    public async Task<ActionResult<ChatResponse>> Ask([FromBody] AskRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
            return BadRequest("Question is required");

        // Get or create conversation
        Conversation conversation;
        List<Message>? history = null;

        if (request.ConversationId.HasValue)
        {
            conversation = await _context.Conversations
                .Include(c => c.Messages.OrderBy(m => m.CreatedAt))
                .FirstOrDefaultAsync(c => c.Id == request.ConversationId.Value)
                ?? throw new InvalidOperationException("Conversation not found");

            history = conversation.Messages.TakeLast(10).ToList();
        }
        else
        {
            conversation = new Conversation
            {
                Title = request.Question.Length > 50
                    ? request.Question.Substring(0, 50) + "..."
                    : request.Question
            };
            _context.Conversations.Add(conversation);
        }

        // Search for relevant chunks
        var relevantChunks = await _vectorSearchService.SearchAsync(request.Question);

        // Generate answer using Claude
        var answer = await _claudeService.GenerateAnswerAsync(request.Question, relevantChunks, history);

        // Add messages to conversation (EF Core handles the FK)
        conversation.Messages.Add(new Message
        {
            Role = "user",
            Content = request.Question
        });

        conversation.Messages.Add(new Message
        {
            Role = "assistant",
            Content = answer,
            Sources = JsonSerializer.Serialize(relevantChunks)
        });

        await _context.SaveChangesAsync();

        return Ok(new ChatResponse
        {
            Answer = answer,
            ConversationId = conversation.Id,
            Sources = relevantChunks.Select(c => new SourceReference
            {
                DocumentId = c.DocumentId,
                Text = c.Text.Length > 200 ? c.Text.Substring(0, 200) + "..." : c.Text,
                Similarity = c.Similarity
            }).ToList()
        });
    }
}
