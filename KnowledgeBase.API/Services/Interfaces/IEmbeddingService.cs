using Pgvector;

namespace KnowledgeBase.API.Services.Interfaces;

public interface IEmbeddingService
{
    Task<List<Vector>> GenerateEmbeddingsAsync(List<string> texts);
}
