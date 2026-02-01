using KnowledgeBase.API.Data.Entities;

namespace KnowledgeBase.API.Services.Interfaces;

public interface IDocumentProcessor
{
    Task<Document> ProcessDocumentAsync(Stream fileStream, string filename, string contentType);
    string ExtractText(Stream fileStream, string contentType);
    List<string> ChunkText(string text);
}
