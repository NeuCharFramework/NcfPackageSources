# Senparc.Xncf.KnowledgeBase

`Senparc.Xncf.KnowledgeBase` is an NCF module for managing knowledge-base collections, source chunks, imported content, and retrieval/search workflows.

## Features

- Manages `KnowledgeBase` and `KnowledgeBaseItem` records through application services and DTOs.
- Supports text/file import, chunk listing/deletion, collection management, and retrieval testing.
- Exposes request/response records such as `CreateCollectionRequest`, `ImportTextRequest`, `SearchRequest`, and `SearchResponse`.
- Uses NCF module localization, authorization areas, and provider-specific EF Core contexts.

## Installation

```xml
<PackageReference Include="Senparc.Xncf.KnowledgeBase" Version="0.26.0-preview3" />
```

## Key API

- `KnowledgeBaseService` and `KnowledgeBaseAppService` manage collections.
- `KnowledgeBaseItemService` and `KnowledgeBaseItemAppService` manage source items and chunks.
- `SearchRequest`/`SearchResponse` define retrieval calls; `ListChunksRequest` and `ListChunksResponse` define chunk browsing.
- `ImportTextRequest`, `ImportFilesRequest`, and `ImportTextResponse` model ingestion.
- `RecallTestAppService` and `RecallTestRequest` support retrieval evaluation.

The module does not choose a vector store, embedding provider, or document permission policy by itself. Keep source access and search results tenant-scoped, and do not index secrets or restricted documents without an explicit policy.
