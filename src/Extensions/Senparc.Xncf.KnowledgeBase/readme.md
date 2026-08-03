# Senparc.Xncf.KnowledgeBase

`Senparc.Xncf.KnowledgeBase` is an NCF module for importing text sources, publishing versioned vector collections, and providing RAG retrieval to other XNCF modules.

## Features

- Stores inline text and FileManager source IDs as independently traceable chunks.
- Extracts supported source files through FileManager, then performs one bounded, overlapping chunk pass.
- Publishes a new timestamped vector collection only after every chunk has been written successfully, so readers never see a half-built collection.
- Uses the configured `TextEmbedding` model for both indexing and retrieval and exposes source names in recall results.
- Provides bounded retry, top-K limits, RAG context length limits, and fail-fast validation for unsupported vector stores.
- Protects management, import, and recall APIs with the NCF `AdminOnly` policy.
- Includes matching migrations for Dm, MySql, Oracle, PostgreSQL, SqlServer, and Sqlite.

## Installation

```xml
<PackageReference Include="Senparc.Xncf.KnowledgeBase" Version="0.4.0-preview5" />
```

## Key API

- `KnowledgeBaseService.CreateOrUpdateAsync` synchronizes metadata, inline content, and selected FileManager files.
- `KnowledgeBaseService.EmbeddingKnowledgeBaseAsync` builds and atomically publishes a new vector collection.
- `KnowledgeBaseService.RecallTestAsync` and `BuildRagContextAsync` provide retrieval and bounded context generation.
- `KnowledgeBaseItemAppService` supports management-page source/chunk inspection.
- `RecallTestAppService` supports administrator retrieval evaluation.

## Current boundaries

- Persistent KnowledgeBase retrieval currently supports Redis and Qdrant. In-memory stores are rejected because they do not survive a request-scoped runner; other vector providers require an AgentKernel adapter before they can be enabled safely.
- PDF, legacy Office, images, audio, OCR, web crawling, automatic collection cleanup, reranking, and document-level ACL filtering are not implemented.
- A FileManager source is ingested as a snapshot. Deleting or changing the original file does not silently rewrite an already published knowledge collection; re-import and re-embed explicitly.
- Vector-store and model credentials remain host configuration. Do not index secrets or restricted documents without a tenant/document permission policy.
