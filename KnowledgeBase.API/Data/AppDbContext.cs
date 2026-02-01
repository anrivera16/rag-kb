using Microsoft.EntityFrameworkCore;
using KnowledgeBase.API.Data.Entities;

namespace KnowledgeBase.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Document configuration
        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(d => d.Filename).HasMaxLength(500).IsRequired();
            entity.Property(d => d.FileType).HasMaxLength(50);
            entity.Property(d => d.Metadata).HasColumnType("jsonb");
        });

        // DocumentChunk configuration
        modelBuilder.Entity<DocumentChunk>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(c => c.ChunkText).IsRequired();
            entity.Property(c => c.Embedding).HasColumnType("vector(1024)");
            entity.Property(c => c.Metadata).HasColumnType("jsonb");

            entity.HasOne(c => c.Document)
                .WithMany(d => d.Chunks)
                .HasForeignKey(c => c.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(c => c.Embedding)
                .HasMethod("ivfflat")
                .HasOperators("vector_cosine_ops")
                .HasStorageParameter("lists", 100);
        });

        // Conversation configuration
        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(c => c.Title).HasMaxLength(500);
        });

        // Message configuration
        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(m => m.Role).HasMaxLength(20).IsRequired();
            entity.Property(m => m.Content).IsRequired();
            entity.Property(m => m.Sources).HasColumnType("jsonb");

            entity.HasOne(m => m.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
