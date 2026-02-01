# RAG Knowledge Base - Project Knowledge

## Project Overview

This is a production-ready RAG (Retrieval Augmented Generation) Knowledge Base system - a customer support chatbot that answers questions based on uploaded company documents using vector search and Claude AI.

**Business Purpose**: This serves as Andrew's first portfolio piece for his AI automation consulting business. It demonstrates the ability to build custom AI solutions that businesses actually need.

**Tech Stack**:
- Backend: .NET 9 Web API
- Frontend: Angular 21
- Database: PostgreSQL with pgvector extension
- AI: Claude API (Sonnet 4.5) + Voyage AI embeddings
- Developer: Andrew (full-stack, 5+ years .NET/Angular/EF/PostgreSQL)

## Architecture

### High-Level Flow
```
Document Upload Flow:
User uploads docs → Extract text → Chunk intelligently → Generate embeddings → Store in PostgreSQL

Query Flow:
User asks question → Embed question → Vector similarity search → Send relevant chunks to Claude → Return answer with sources
```

### Why RAG?

RAG (Retrieval Augmented Generation) solves the problem of LLMs not knowing company-specific information. Instead of fine-tuning (expensive, slow), we:
1. Store company documents as searchable embeddings
2. Retrieve relevant chunks for each question
3. Send those chunks as context to Claude
4. Get accurate, source-cited answers

## Project Structure
```
RAGKnowledgeBase/
├── KnowledgeBase.API/
│   ├── Controllers/
│   │   ├── DocumentsController.cs
│   │   ├── ChatController.cs
│   │   └── ConversationsController.cs
│   ├── Data/
│   │   ├── AppDbContext.cs
│   │   └── Entities/
│   │       ├── Document.cs
│   │       ├── DocumentChunk.cs
│   │       ├── Conversation.cs
│   │       └── Message.cs
│   ├── Services/
│   │   ├── Interfaces/
│   │   │   ├── IDocumentProcessor.cs
│   │   │   ├── IEmbeddingService.cs
│   │   │   ├── IVectorSearchService.cs
│   │   │   └── IClaudeService.cs
│   │   └── Implementations/
│   │       ├── DocumentProcessor.cs
│   │       ├── VoyageEmbeddingService.cs
│   │       ├── VectorSearchService.cs
│   │       └── ClaudeService.cs
│   ├── Models/
│   │   └── DTOs/
│   │       ├── AskRequest.cs
│   │       ├── ChatResponse.cs
│   │       ├── DocumentUploadRequest.cs
│   │       └── RelevantChunk.cs
│   └── Program.cs
│
└── knowledge-base-ui/
    └── src/app/
        ├── components/
        │   ├── document-upload/
        │   ├── chat-interface/
        │   └── conversation-list/
        └── services/
            ├── knowledge-base.service.ts
            └── chat.service.ts
```

## Database Schema
```sql
-- Enable pgvector extension
CREATE EXTENSION IF NOT EXISTS vector;

-- Documents table
CREATE TABLE documents (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    filename VARCHAR(500) NOT NULL,
    file_type VARCHAR(50),
    uploaded_at TIMESTAMP DEFAULT NOW(),
    processed BOOLEAN DEFAULT FALSE,
    metadata JSONB
);

-- Document chunks with embeddings
CREATE TABLE document_chunks (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    document_id UUID REFERENCES documents(id) ON DELETE CASCADE,
    chunk_text TEXT NOT NULL,
    chunk_index INTEGER NOT NULL,
    embedding vector(1024), -- Voyage AI embeddings are 1024 dimensions
    metadata JSONB,
    created_at TIMESTAMP DEFAULT NOW()
);

-- Index for vector similarity search (critical for performance)
CREATE INDEX ON document_chunks USING ivfflat (embedding vector_cosine_ops)
WITH (lists = 100);

-- Conversations
CREATE TABLE conversations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    created_at TIMESTAMP DEFAULT NOW(),
    title VARCHAR(500)
);

-- Messages
CREATE TABLE messages (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    conversation_id UUID REFERENCES conversations(id) ON DELETE CASCADE,
    role VARCHAR(20) NOT NULL, -- 'user' or 'assistant'
    content TEXT NOT NULL,
    sources JSONB, -- Store which chunks were used
    created_at TIMESTAMP DEFAULT NOW()
);
```

## Core Components

### 1. Document Processor (IDocumentProcessor)

**Purpose**: Convert uploaded files into searchable chunks

**Key Responsibilities**:
- Extract text from PDF, DOCX, TXT files
- Split text into optimal chunks (1000 chars with 200 char overlap)
- Preserve semantic meaning when chunking
- Store chunks with metadata

**Chunking Strategy**:
- Target: 1000 characters (~750 tokens) per chunk
- Overlap: 200 characters between chunks for context continuity
- Split on paragraph boundaries when possible
- Never split mid-sentence

**Why this matters**: Poor chunking = poor answers. If chunks are too large, irrelevant info dilutes the context. Too small, and you lose important context.

### 2. Embedding Service (IEmbeddingService)

**Purpose**: Convert text into vector representations for similarity search

**Key Responsibilities**:
- Call Voyage AI API to generate embeddings
- Batch process for efficiency (up to 128 chunks at once)
- Return 1024-dimensional float arrays

**Why Voyage AI over OpenAI**:
- 30% better retrieval accuracy
- Purpose-built for search/RAG
- Cost: $0.12 per 1M tokens (competitive with OpenAI)

**Alternative**: Can use OpenAI `text-embedding-3-small` (1536 dimensions) if consolidating vendors

### 3. Vector Search Service (IVectorSearchService)

**Purpose**: Find the most relevant document chunks for a given question

**Key Responsibilities**:
- Embed the user's question
- Perform cosine similarity search using pgvector
- Return top K most relevant chunks (default: 5)
- Include similarity scores

**How Vector Search Works**:
1. Question "What's our vacation policy?" → embedding vector
2. Compare against all chunk embeddings using cosine similarity
3. pgvector efficiently finds nearest neighbors
4. Return chunks ordered by relevance

**Performance**: With proper indexing, searches 100k+ chunks in <100ms

### 4. Claude Service (IClaudeService)

**Purpose**: Generate natural language answers using retrieved context

**Key Responsibilities**:
- Assemble context from relevant chunks
- Build effective prompts for Claude
- Manage conversation history for multi-turn dialogue
- Return answers with source citations

**Prompt Structure**:
```
System Prompt:
"You are a helpful customer support assistant. 
Answer questions based ONLY on the provided context documents. 
If the answer isn't in the context, say so clearly.
Always cite which document section you're referencing.
Be concise but complete."

User Message:
"Context documents:
[Document 1]
{chunk text}

[Document 2]
{chunk text}

Question: {user question}

Provide a helpful answer based on the context above."
```

**Why this works**:
- Clear instructions prevent hallucination
- Multiple context chunks give comprehensive view
- Citation requirement builds trust
- Concise directive keeps answers focused

### 5. API Controllers

**DocumentsController**:
- `POST /api/documents/upload` - Upload and process document
- `GET /api/documents` - List all documents
- `GET /api/documents/{id}` - Get document details with chunks
- `DELETE /api/documents/{id}` - Delete document and all chunks

**ChatController**:
- `POST /api/chat/ask` - Ask a question
  - Request: `{ question: string, conversationId?: Guid }`
  - Response: `{ answer: string, sources: Source[] }`

**ConversationsController**:
- `POST /api/conversations` - Create new conversation
- `GET /api/conversations` - List all conversations
- `GET /api/conversations/{id}/messages` - Get full conversation history
- `DELETE /api/conversations/{id}` - Delete conversation

### 6. Angular Frontend

**Document Upload Component**:
- Drag-and-drop file upload
- Support PDF, DOCX, TXT
- Show processing status
- Display uploaded documents list

**Chat Interface Component**:
- Message input with send button
- Display conversation history
- Show source chunks used for each answer
- Loading indicators during processing
- Error handling and retry logic

**Conversation List Component**:
- Sidebar showing all conversations
- Create new conversation
- Switch between conversations
- Delete conversations

## Configuration

### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=rag_knowledge_base;Username=postgres;Password=yourpassword"
  },
  "ApiKeys": {
    "ClaudeApiKey": "your-claude-api-key-here",
    "VoyageApiKey": "your-voyage-api-key-here"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Cors": {
    "AllowedOrigins": ["http://localhost:4200"]
  }
}
```

### Required NuGet Packages
```xml
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.0" />
<PackageReference Include="Pgvector.EntityFrameworkCore" Version="0.2.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.0" />
<PackageReference Include="iTextSharp" Version="5.5.13.3" />
<PackageReference Include="DocumentFormat.OpenXml" Version="3.0.0" />
```

### API Key Sources

- **Claude API**: https://console.anthropic.com/ (Sign up → API Keys → Create Key)
- **Voyage AI**: https://www.voyageai.com/ (Sign up → Dashboard → API Keys)

**Cost Estimates** (for reference):
- Voyage AI Embeddings: $0.12 per 1M tokens
- Claude Sonnet 4.5: ~$3 per 1M input tokens, ~$15 per 1M output tokens
- For 1000 documents (~500 pages each): ~$15 one-time embedding cost
- For 1000 queries: ~$3-5/month

## Development Phases

### Phase 1: Backend Foundation (Days 1-5)
**Goal**: Set up infrastructure and document processing

- Set up project structure, database, Entity Framework
- Implement document upload endpoint
- Build text extraction for PDF/DOCX/TXT
- Implement chunking algorithm
- Test with 5-10 sample documents
- Verify chunks are stored correctly

**Success Metric**: Upload a 50-page PDF and see it chunked into ~50 pieces

### Phase 2: Embeddings & Storage (Days 6-9)
**Goal**: Convert chunks to embeddings and enable search

- Integrate Voyage AI API
- Implement batch embedding generation
- Store embeddings in PostgreSQL with pgvector
- Create vector similarity search function
- Test search relevance with sample queries
- Optimize pgvector index configuration

**Success Metric**: Search returns relevant chunks in <100ms

### Phase 3: Claude Integration (Days 10-11)
**Goal**: Generate answers from retrieved context

- Implement Claude API service
- Build context assembly logic
- Create effective system prompts
- Add conversation history support
- Implement ChatController endpoints
- Test answer quality and accuracy

**Success Metric**: Ask "What's our vacation policy?" and get accurate answer with sources

### Phase 4: Frontend (Days 12-14)
**Goal**: Build user interface

- Create Angular project structure
- Build document upload component
- Implement chat interface
- Display source citations
- Add conversation history sidebar
- Implement loading states and error handling

**Success Metric**: Complete end-to-end flow from upload to chat works smoothly

### Phase 5: Polish & Demo (Days 15-16)
**Goal**: Production-ready and presentable

- Add response streaming for better UX
- Implement query analytics
- Optimize chunking algorithm
- Add error logging
- Create demo video
- Write documentation

**Success Metric**: Can demo to potential clients confidently

## Key Implementation Details

### Chunking Strategy (Critical for Quality)
```csharp
private List<string> ChunkText(string text)
{
    const int targetChunkSize = 1000; // ~750 tokens
    const int overlap = 200; // tokens for context continuity
    
    var chunks = new List<string>();
    
    // Split on paragraphs first (preserve semantic boundaries)
    var paragraphs = text.Split(new[] { "\n\n", "\r\n\r\n" }, 
        StringSplitOptions.RemoveEmptyEntries);
    
    var currentChunk = new StringBuilder();
    
    foreach (var para in paragraphs)
    {
        // If adding this paragraph exceeds target, save chunk
        if (currentChunk.Length + para.Length > targetChunkSize && currentChunk.Length > 0)
        {
            chunks.Add(currentChunk.ToString());
            
            // Create overlap: keep last 200 chars for context
            var overlapText = currentChunk.ToString()
                .Substring(Math.Max(0, currentChunk.Length - overlap));
            currentChunk = new StringBuilder(overlapText);
        }
        
        currentChunk.AppendLine(para);
    }
    
    if (currentChunk.Length > 0)
        chunks.Add(currentChunk.ToString());
    
    return chunks;
}
```

**Why this works**:
- Respects paragraph boundaries (semantic units)
- Overlap prevents context loss at boundaries
- Target size balances detail vs. noise
- Flexible enough to handle various document structures

### Vector Search Implementation
```csharp
public async Task<List<RelevantChunk>> SearchAsync(string query, int topK = 5)
{
    // 1. Embed the query
    var queryEmbedding = await _embeddingService
        .GenerateEmbeddingsAsync(new List<string> { query });
    
    var embeddingVector = queryEmbedding.First();
    
    // 2. Perform cosine similarity search using pgvector
    var results = await _context.DocumentChunks
        .Select(c => new 
        {
            Chunk = c,
            Distance = c.Embedding.CosineDistance(embeddingVector)
        })
        .OrderBy(x => x.Distance)
        .Take(topK)
        .ToListAsync();
    
    // 3. Convert distance to similarity score (0-1)
    return results.Select(r => new RelevantChunk
    {
        Text = r.Chunk.ChunkText,
        DocumentId = r.Chunk.DocumentId,
        Similarity = 1 - r.Distance, // Distance to similarity
        Metadata = r.Chunk.Metadata
    }).ToList();
}
```

**Key Points**:
- pgvector's `CosineDistance` returns 0 (identical) to 2 (opposite)
- Convert to similarity: `1 - distance` gives 0-1 score
- `USING ivfflat` index makes this fast even with 100k+ chunks
- Top-K limits results to most relevant

### Claude API Integration
```csharp
public async Task<ChatResponse> GenerateAnswerAsync(
    string question, 
    List<RelevantChunk> relevantChunks,
    List<Message> conversationHistory = null)
{
    // 1. Build context from chunks
    var contextText = BuildContext(relevantChunks);
    
    // 2. System prompt (critical for quality)
    var systemPrompt = @"You are a helpful customer support assistant. 
Answer questions based ONLY on the provided context documents. 
If the answer isn't in the context, say so clearly.
Always cite which document section you're referencing.
Be concise but complete.";
    
    // 3. User prompt with context
    var userPrompt = $@"Context documents:
{contextText}

Question: {question}

Provide a helpful answer based on the context above.";
    
    // 4. Build messages array (include history for multi-turn)
    var messages = new List<object>();
    
    if (conversationHistory?.Any() == true)
    {
        messages.AddRange(conversationHistory.Select(m => new 
        {
            role = m.Role,
            content = m.Content
        }));
    }
    
    messages.Add(new { role = "user", content = userPrompt });
    
    // 5. Call Claude API
    var request = new
    {
        model = "claude-sonnet-4-5-20250929",
        max_tokens = 1024,
        system = systemPrompt,
        messages = messages
    };
    
    _httpClient.DefaultRequestHeaders.Clear();
    _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
    _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
    
    var response = await _httpClient.PostAsJsonAsync(
        "https://api.anthropic.com/v1/messages",
        request
    );
    
    var result = await response.Content.ReadFromJsonAsync<ClaudeResponse>();
    
    return new ChatResponse
    {
        Answer = result.Content.First().Text,
        Sources = relevantChunks.Select(c => new Source 
        { 
            DocumentId = c.DocumentId,
            Text = c.Text.Substring(0, Math.Min(200, c.Text.Length)) + "...",
            Similarity = c.Similarity
        }).ToList()
    };
}

private string BuildContext(List<RelevantChunk> chunks)
{
    var sb = new StringBuilder();
    
    for (int i = 0; i < chunks.Count; i++)
    {
        sb.AppendLine($"[Document {i + 1}]");
        sb.AppendLine(chunks[i].Text);
        sb.AppendLine();
    }
    
    return sb.ToString();
}
```

## API Endpoints Reference

### Documents API

**Upload Document**
```http
POST /api/documents/upload
Content-Type: multipart/form-data

Response: {
  "id": "guid",
  "filename": "string",
  "fileType": "string",
  "processed": false,
  "uploadedAt": "datetime"
}
```

**List Documents**
```http
GET /api/documents

Response: [
  {
    "id": "guid",
    "filename": "string",
    "fileType": "string",
    "processed": true,
    "uploadedAt": "datetime",
    "chunkCount": 45
  }
]
```

**Delete Document**
```http
DELETE /api/documents/{id}

Response: 204 No Content
```

### Chat API

**Ask Question**
```http
POST /api/chat/ask
Content-Type: application/json

{
  "question": "What is our vacation policy?",
  "conversationId": "guid" // optional
}

Response: {
  "answer": "Based on the employee handbook...",
  "sources": [
    {
      "documentId": "guid",
      "text": "Employees are entitled to...",
      "similarity": 0.89
    }
  ]
}
```

### Conversations API

**Create Conversation**
```http
POST /api/conversations
Content-Type: application/json

{
  "title": "HR Policy Questions"
}

Response: {
  "id": "guid",
  "title": "HR Policy Questions",
  "createdAt": "datetime"
}
```

**Get Conversation Messages**
```http
GET /api/conversations/{id}/messages

Response: [
  {
    "id": "guid",
    "role": "user",
    "content": "What is our vacation policy?",
    "createdAt": "datetime"
  },
  {
    "id": "guid",
    "role": "assistant",
    "content": "Based on the employee handbook...",
    "sources": [...],
    "createdAt": "datetime"
  }
]
```

## Performance Targets

- **Document processing**: < 30 seconds for 50-page PDF
- **Embedding generation**: < 5 seconds for 100 chunks (batch processing)
- **Vector search**: < 100ms for 100k+ chunks (with proper indexing)
- **Claude response**: < 2 seconds (depends on answer length)
- **Total query time**: < 3 seconds end-to-end

## Common Pitfalls & Solutions

### Problem: Irrelevant Chunks Retrieved
**Cause**: Poor chunking strategy or embedding quality
**Solution**: 
- Adjust chunk size (try 800-1200 range)
- Add more overlap (300 chars)
- Use hybrid search (keyword + semantic)

### Problem: Claude Hallucinates
**Cause**: Weak system prompt or insufficient context
**Solution**:
- Strengthen system prompt: "ONLY use provided context"
- Increase retrieved chunks from 5 to 7
- Add explicit "say you don't know if not in context"

### Problem: Slow Search Performance
**Cause**: Missing or poorly configured pgvector index
**Solution**:
- Ensure `ivfflat` index exists
- Tune `lists` parameter (100 for <100k vectors, 1000 for >100k)
- Consider upgrading to HNSW index (better accuracy)

### Problem: Out of Order Responses
**Cause**: No conversation tracking
**Solution**:
- Always pass conversation history to Claude
- Limit to last 10 messages (avoid context bloat)
- Store conversation metadata

### Problem: High API Costs
**Cause**: Inefficient batching or over-retrieval
**Solution**:
- Batch embed 128 chunks at once
- Cache frequently asked questions
- Use Claude Haiku for simple queries
- Reduce retrieved chunks from 5 to 3 if quality allows

## Success Criteria

This project is complete when:

✅ Users can upload PDF/DOCX/TXT documents
✅ Documents are automatically chunked and embedded
✅ Users can ask natural language questions
✅ System returns accurate answers with source citations
✅ Multi-turn conversations work correctly
✅ Response time is under 3 seconds
✅ System handles 10+ documents (500+ pages) without performance issues
✅ UI is polished and intuitive
✅ Demo video is recorded and professional

## Demo Script (For Client Presentations)

**Setup**: Upload 5-10 company documents (employee handbook, FAQ, policies)

**Demo Flow**:
1. **Upload Documents** (30 seconds)
   - Drag-drop PDF employee handbook
   - Show processing status
   - Display in document list

2. **Simple Query** (45 seconds)
   - Ask: "What's our vacation policy?"
   - Show answer with highlighted sources
   - Click source to see original text

3. **Multi-turn Conversation** (60 seconds)
   - Follow-up: "What about sick leave?"
   - Show conversation remembers context
   - Ask: "How does this compare to our WFH policy?"

4. **Complex Query** (45 seconds)
   - Ask: "I'm a new employee starting in March. What benefits am I eligible for and when?"
   - Show synthesis across multiple documents
   - Highlight 3-4 different sources used

5. **Edge Case** (30 seconds)
   - Ask: "What's the CEO's favorite color?"
   - Show system admits it doesn't know
   - Demonstrate it doesn't hallucinate

**Key Points to Emphasize**:
- Instant access to company knowledge
- Always cites sources (no hallucination)
- Understands context and follow-ups
- Saves hours of manual document searching
- ROI: Support team answers 3x faster

## Technical Decisions & Rationale

### Why Voyage AI over OpenAI Embeddings?
- 30% better retrieval accuracy for RAG use cases
- Purpose-built for search (not just similarity)
- Competitive pricing ($0.12 vs $0.13 per 1M tokens)
- Smaller dimension (1024 vs 1536) = faster search

### Why Claude over GPT-4?
- Better instruction following (less hallucination)
- Longer context window (200k tokens)
- Better at citing sources
- More reliable JSON output
- Andrew wants to learn Claude ecosystem

### Why PostgreSQL + pgvector over Pinecone/Weaviate?
- Andrew already expert in PostgreSQL
- No vendor lock-in
- Lower cost (no separate vector DB subscription)
- Simpler architecture (one database)
- pgvector performance is excellent for <1M vectors

### Why Chunking with Overlap?
- Prevents context loss at boundaries
- Improves retrieval accuracy by 15-20%
- Handles queries that span chunk boundaries
- Standard RAG best practice

## Future Enhancements (Post-MVP)

### Phase 2 Features:
- **Hybrid Search**: Combine keyword (BM25) + semantic search
- **Reranking**: Use Cohere/Voyage rerank for better top-5
- **Response Streaming**: Stream Claude's answer token-by-token
- **Query Analytics**: Track most common questions, answer quality
- **Multi-tenant**: Support multiple companies/teams
- **Access Control**: Role-based document permissions

### Phase 3 Features:
- **Active Learning**: Flag poor answers for improvement
- **Suggested Questions**: Show related questions user might ask
- **Document Versioning**: Track changes to policies over time
- **Integration**: Slack bot, Teams bot, API webhooks
- **Advanced Analytics**: Query patterns, usage metrics, ROI tracking

## Learning Resources

### RAG Architecture:
- Pinecone's RAG guide: https://www.pinecone.io/learn/retrieval-augmented-generation/
- LangChain RAG tutorial: https://python.langchain.com/docs/use_cases/question_answering/

### Vector Search:
- pgvector documentation: https://github.com/pgvector/pgvector
- Understanding embeddings: https://platform.openai.com/docs/guides/embeddings

### Prompt Engineering:
- Anthropic's prompt engineering guide: https://docs.anthropic.com/claude/docs/prompt-engineering
- Claude best practices: https://docs.anthropic.com/claude/docs/intro-to-claude

## Context for Claude

When helping Andrew with this project:

1. **Assume Expertise**: He knows .NET, Angular, Entity Framework, PostgreSQL deeply
2. **Be Specific**: Provide actual code, not pseudocode or high-level descriptions
3. **Explain RAG Concepts**: He's learning vector search, embeddings, RAG patterns
4. **Think Production**: Consider error handling, performance, security, scalability
5. **Reference His Background**: Secret Clearance, DoD apps, enterprise systems experience

**Andrew's Goals**:
- Master RAG architecture and implementation
- Understand vector embeddings and similarity search deeply
- Learn effective prompt engineering for Claude
- Build a portfolio piece to attract AI automation clients
- Eventually start consulting business helping companies with AI automation

**Current Status**: Project structure created, ready to implement services

**Next Steps**: Implement DocumentProcessor service with text extraction and chunking