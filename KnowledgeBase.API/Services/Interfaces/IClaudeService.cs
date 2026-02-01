using KnowledgeBase.API.Data.Entities;
using KnowledgeBase.API.Models.DTOs;

namespace KnowledgeBase.API.Services.Interfaces;

public interface IClaudeService
{
    Task<string> GenerateAnswerAsync(string question, List<RelevantChunk> relevantChunks, List<Message>? conversationHistory = null);
}
