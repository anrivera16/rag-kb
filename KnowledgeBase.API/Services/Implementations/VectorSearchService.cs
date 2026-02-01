using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using KnowledgeBase.API.Data;
using KnowledgeBase.API.Models.DTOs;
using KnowledgeBase.API.Services.Interfaces;

namespace KnowledgeBase.API.Services.Implementations;

public class VectorSearchService : IVectorSearchService
{
    private readonly AppDbContext _context;
    private readonly IEmbeddingService _embeddingService;

    public VectorSearchService(AppDbContext context, IEmbeddingService embeddingService)
    {
        _context = context;
        _embeddingService = embeddingService;
    }

    public async Task<List<RelevantChunk>> SearchAsync(string query, int topK = 5)
    {
        // Generate embedding for the query
        var embeddings = await _embeddingService.GenerateEmbeddingsAsync(new List<string> { query });
        var queryEmbedding = embeddings.First();

        // Perform cosine similarity search
        var results = await _context.DocumentChunks
            .Where(c => c.Embedding != null)
            .Select(c => new
            {
                c.DocumentId,
                c.ChunkText,
                Distance = c.Embedding!.CosineDistance(queryEmbedding)
            })
            .OrderBy(x => x.Distance)
            .Take(topK)
            .ToListAsync();

        // Convert distance to similarity (0-1 scale)
        return results.Select(r => new RelevantChunk
        {
            DocumentId = r.DocumentId,
            Text = r.ChunkText,
            Similarity = 1 - r.Distance
        }).ToList();
    }
}
