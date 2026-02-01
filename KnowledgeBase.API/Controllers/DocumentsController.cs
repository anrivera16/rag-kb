using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KnowledgeBase.API.Data;
using KnowledgeBase.API.Models.DTOs;
using KnowledgeBase.API.Services.Interfaces;

namespace KnowledgeBase.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IDocumentProcessor _documentProcessor;

    public DocumentsController(AppDbContext context, IDocumentProcessor documentProcessor)
    {
        _context = context;
        _documentProcessor = documentProcessor;
    }

    [HttpPost("upload")]
    public async Task<ActionResult<DocumentDto>> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file provided");

        var allowedTypes = new[]
        {
            "application/pdf",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "text/plain"
        };

        if (!allowedTypes.Contains(file.ContentType))
            return BadRequest("Unsupported file type. Allowed: PDF, DOCX, TXT");

        using var stream = file.OpenReadStream();
        var document = await _documentProcessor.ProcessDocumentAsync(stream, file.FileName, file.ContentType);

        var chunkCount = await _context.DocumentChunks.CountAsync(c => c.DocumentId == document.Id);

        return Ok(new DocumentDto
        {
            Id = document.Id,
            Filename = document.Filename,
            FileType = document.FileType,
            UploadedAt = document.UploadedAt,
            Processed = document.Processed,
            ChunkCount = chunkCount
        });
    }

    [HttpGet]
    public async Task<ActionResult<List<DocumentDto>>> GetAll()
    {
        var documents = await _context.Documents
            .Select(d => new DocumentDto
            {
                Id = d.Id,
                Filename = d.Filename,
                FileType = d.FileType,
                UploadedAt = d.UploadedAt,
                Processed = d.Processed,
                ChunkCount = d.Chunks.Count
            })
            .ToListAsync();

        return Ok(documents);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DocumentDto>> GetById(Guid id)
    {
        var document = await _context.Documents
            .Include(d => d.Chunks)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (document == null)
            return NotFound();

        return Ok(new DocumentDto
        {
            Id = document.Id,
            Filename = document.Filename,
            FileType = document.FileType,
            UploadedAt = document.UploadedAt,
            Processed = document.Processed,
            ChunkCount = document.Chunks.Count
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var document = await _context.Documents.FindAsync(id);

        if (document == null)
            return NotFound();

        _context.Documents.Remove(document);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
