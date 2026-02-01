# RAG Knowledge Base

A customer support chatbot that answers questions from uploaded company documents using vector search and AI.

## How It Works

1. **Upload documents** (PDF, DOCX, TXT)
2. **Text is chunked** and converted to vector embeddings
3. **User asks a question** → finds relevant chunks via vector similarity
4. **Claude AI** generates an answer using only the retrieved context

## Tech Stack

- **Backend**: .NET 9 / ASP.NET Core Web API
- **Frontend**: Angular 21
- **Database**: PostgreSQL + pgvector
- **AI**: Claude (answers) + Voyage AI (embeddings)

## Setup

```bash
# Backend
dotnet run --project KnowledgeBase.API

# Frontend
cd knowledge-base-ui && npm start
```

Requires API keys in `appsettings.json`:
- `ApiKeys:ClaudeApiKey`
- `ApiKeys:VoyageApiKey`

## Architecture

```
Upload: PDF → Extract Text → Chunk → Embed (Voyage) → Store in pgvector

Query:  Question → Embed → Vector Search → Top 5 Chunks → Claude → Answer
```
