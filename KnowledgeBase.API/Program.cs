using System.Text;
using Microsoft.EntityFrameworkCore;
using KnowledgeBase.API.Data;
using KnowledgeBase.API.Services.Interfaces;
using KnowledgeBase.API.Services.Implementations;

// Register additional encodings required by iTextSharp for PDF processing
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var builder = WebApplication.CreateBuilder(args);

// Database with pgvector
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        o => o.UseVector()
    )
);

// Services
builder.Services.AddScoped<IEmbeddingService, VoyageEmbeddingService>();
builder.Services.AddScoped<IDocumentProcessor, DocumentProcessor>();
builder.Services.AddScoped<IVectorSearchService, VectorSearchService>();
builder.Services.AddScoped<IClaudeService, ClaudeService>();

// CORS for Angular frontend
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors();
app.MapControllers();

app.Run();
