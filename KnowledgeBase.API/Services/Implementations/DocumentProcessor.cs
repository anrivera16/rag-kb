using System.Text;
using DocumentFormat.OpenXml.Packaging;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using KnowledgeBase.API.Data;
using KnowledgeBase.API.Data.Entities;
using KnowledgeBase.API.Services.Interfaces;

namespace KnowledgeBase.API.Services.Implementations;

public class DocumentProcessor : IDocumentProcessor
{
    private readonly AppDbContext _context;
    private readonly IEmbeddingService _embeddingService;
    private const int TargetChunkSize = 1000;
    private const int Overlap = 200;

    public DocumentProcessor(AppDbContext context, IEmbeddingService embeddingService)
    {
        _context = context;
        _embeddingService = embeddingService;
    }

    public async Task<Document> ProcessDocumentAsync(Stream fileStream, string filename, string contentType)
    {
        var text = ExtractText(fileStream, contentType);
        var chunkTexts = ChunkText(text);

        // Generate embeddings for all chunks
        var embeddings = await _embeddingService.GenerateEmbeddingsAsync(chunkTexts);

        var document = new Document
        {
            Filename = filename,
            FileType = contentType,
            Processed = true
        };

        // Add chunks to document's collection (EF Core handles the FK)
        for (int i = 0; i < chunkTexts.Count; i++)
        {
            document.Chunks.Add(new DocumentChunk
            {
                ChunkText = chunkTexts[i],
                ChunkIndex = i,
                Embedding = embeddings[i]
            });
        }

        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        return document;
    }

    public string ExtractText(Stream fileStream, string contentType)
    {
        return contentType switch
        {
            "application/pdf" => ExtractFromPdf(fileStream),
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => ExtractFromDocx(fileStream),
            "text/plain" => ExtractFromTxt(fileStream),
            _ => throw new NotSupportedException($"Content type '{contentType}' is not supported")
        };
    }

    private string ExtractFromPdf(Stream fileStream)
    {
        using var memoryStream = new MemoryStream();
        fileStream.CopyTo(memoryStream);
        memoryStream.Position = 0;

        using var reader = new PdfReader(memoryStream);
        var sb = new StringBuilder();

        for (int i = 1; i <= reader.NumberOfPages; i++)
        {
            sb.Append(PdfTextExtractor.GetTextFromPage(reader, i));
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private string ExtractFromDocx(Stream fileStream)
    {
        using var memoryStream = new MemoryStream();
        fileStream.CopyTo(memoryStream);
        memoryStream.Position = 0;

        using var doc = WordprocessingDocument.Open(memoryStream, false);
        var body = doc.MainDocumentPart?.Document.Body;
        return body?.InnerText ?? string.Empty;
    }

    private string ExtractFromTxt(Stream fileStream)
    {
        using var reader = new StreamReader(fileStream);
        return reader.ReadToEnd();
    }

    public List<string> ChunkText(string text)
    {
        var chunks = new List<string>();

        // Split on single or double newlines to handle PDF text better
        var paragraphs = text.Split(new[] { "\n\n", "\r\n\r\n", "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
        var currentChunk = new StringBuilder();

        foreach (var para in paragraphs)
        {
            // If this single paragraph is larger than target, split it by sentences
            if (para.Length > TargetChunkSize)
            {
                // First, flush current chunk if any
                if (currentChunk.Length > 0)
                {
                    chunks.Add(currentChunk.ToString().Trim());
                    var overlapText = currentChunk.ToString();
                    overlapText = overlapText.Substring(Math.Max(0, overlapText.Length - Overlap));
                    currentChunk = new StringBuilder(overlapText);
                }

                // Split large paragraph into smaller pieces
                var remaining = para;
                while (remaining.Length > 0)
                {
                    if (currentChunk.Length + remaining.Length <= TargetChunkSize)
                    {
                        currentChunk.AppendLine(remaining);
                        break;
                    }

                    // Find a good break point (end of sentence or space)
                    var spaceAvailable = TargetChunkSize - currentChunk.Length;
                    var breakPoint = FindBreakPoint(remaining, spaceAvailable);

                    currentChunk.Append(remaining.Substring(0, breakPoint));
                    chunks.Add(currentChunk.ToString().Trim());

                    // Create overlap
                    var overlapText = currentChunk.ToString();
                    overlapText = overlapText.Substring(Math.Max(0, overlapText.Length - Overlap));
                    currentChunk = new StringBuilder(overlapText);

                    remaining = remaining.Substring(breakPoint).TrimStart();
                }
            }
            else if (currentChunk.Length + para.Length > TargetChunkSize && currentChunk.Length > 0)
            {
                chunks.Add(currentChunk.ToString().Trim());

                // Create overlap: keep last N chars for context
                var overlapText = currentChunk.ToString();
                overlapText = overlapText.Substring(Math.Max(0, overlapText.Length - Overlap));
                currentChunk = new StringBuilder(overlapText);
                currentChunk.AppendLine(para);
            }
            else
            {
                currentChunk.AppendLine(para);
            }
        }

        if (currentChunk.Length > 0)
        {
            var finalChunk = currentChunk.ToString().Trim();
            if (finalChunk.Length > 0)
            {
                chunks.Add(finalChunk);
            }
        }

        return chunks;
    }

    private int FindBreakPoint(string text, int maxLength)
    {
        if (maxLength >= text.Length) return text.Length;
        if (maxLength <= 0) maxLength = TargetChunkSize;

        // Look for sentence endings first (. ! ?)
        var searchEnd = Math.Min(maxLength, text.Length);
        for (int i = searchEnd - 1; i > maxLength / 2; i--)
        {
            if (text[i] == '.' || text[i] == '!' || text[i] == '?')
            {
                return i + 1;
            }
        }

        // Fall back to space
        for (int i = searchEnd - 1; i > maxLength / 2; i--)
        {
            if (char.IsWhiteSpace(text[i]))
            {
                return i + 1;
            }
        }

        // No good break point, just cut at max length
        return searchEnd;
    }
}
