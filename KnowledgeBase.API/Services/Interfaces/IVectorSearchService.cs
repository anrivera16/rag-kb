using KnowledgeBase.API.Models.DTOs;

namespace KnowledgeBase.API.Services.Interfaces;

public interface IVectorSearchService
{
    Task<List<RelevantChunk>> SearchAsync(string query, int topK = 5);
}
