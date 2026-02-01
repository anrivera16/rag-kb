# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

RAG Knowledge Base — a full-stack customer support chatbot that answers questions from uploaded company documents using vector search and Claude AI. Built as a portfolio piece for an AI automation consulting business.

## Tech Stack

- **Backend**: C# / .NET 9.0 / ASP.NET Core Web API / Entity Framework Core 8.0
- **Frontend**: Angular 21 / TypeScript / SCSS
- **Database**: PostgreSQL with pgvector extension (1024-dimensional embeddings)
- **AI Services**: Claude Sonnet 4.5 (answers), Voyage AI (embeddings)

## Build & Run Commands

```bash
# Backend (.NET)
dotnet build                          # Build
dotnet run --project KnowledgeBase.API  # Run API server

# Frontend (Angular)
cd knowledge-base-ui
npm start                             # Dev server on localhost:4200
npm run build                         # Production build
npm test                              # Unit tests (Karma/Jasmine)
```

## Architecture

Two-service architecture: Angular SPA calls .NET Web API over HTTP (CORS configured for localhost:4200).

**Document Upload Flow**: File → DocumentsController → DocumentProcessor (extract text, chunk into 1000-char segments with 200-char overlap) → VoyageEmbeddingService (generate vectors) → PostgreSQL/pgvector

**Chat Flow**: Question → ChatController → VoyageEmbeddingService (embed query) → VectorSearchService (cosine similarity, top 5 chunks) → ClaudeService (generate answer with source citations) → Response stored in conversations/messages tables

**Backend layers**: Controllers → Service interfaces (IDocumentProcessor, IEmbeddingService, IVectorSearchService, IClaudeService) → Implementations → EF Core DbContext → PostgreSQL

**Database entities**: documents, document_chunks (with vector embeddings), conversations, messages

## Key Implementation Details

- Chunking splits on paragraph boundaries, never mid-sentence
- Vector search uses pgvector `CosineDistance`, converts to similarity via `1 - distance`
- IVFFlat index with `lists=100` (use 1000 for >100k vectors)
- Claude system prompt must explicitly prevent hallucination and require source citations
- Max 128 chunks per Voyage AI embedding batch
- API keys needed: Anthropic (Claude) and Voyage AI — stored in appsettings, not committed

## Solution Structure

- `RAGKnowledgeBase.sln` — Solution file
- `KnowledgeBase.API/` — .NET backend (Controllers, Services, Data/Entities, Models/DTOs)
- `knowledge-base-ui/` — Angular frontend (components: document-upload, chat-interface, conversation-list; services: knowledge-base, chat)
- `Objectives.md` — Comprehensive project spec with code examples and architectural decisions
