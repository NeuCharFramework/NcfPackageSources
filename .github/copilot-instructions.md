# GitHub Copilot Instructions for NeuCharFramework (NCF)

## Project Overview
This project is based on **NeuCharFramework (NCF)**, a modular .NET framework.
The current focus is on upgrading the **KnowledgeBase** module to implement a complete **RAG (Retrieval-Augmented Generation)** system.

## Architecture & Modules
The system is composed of **Xncf Modules** located in `src/Extensions/`.
Key modules for the RAG implementation:

1.  **Senparc.Xncf.KnowledgeBase** (Target Module)
    -   **Role**: Orchestrates the RAG process, manages knowledge bases and documents.
    -   **Responsibilities**:
        -   Manage `KnowledgeBases` (Indices) and `KnowledgeBasesDetail` (Documents/Chunks).
        -   Handle file processing (splitting, cleaning).
        -   Coordinate embedding generation and vector storage.
        -   Execute RAG queries (Retrieve + Generate).
    -   **Key Files**: `Domain/Models/DatabaseModel/KnowledgeBases.cs`, `Domain/Services/KnowledgeBasesService.cs`.

2.  **Senparc.Xncf.AIKernel** (Infrastructure Module)
    -   **Role**: Provides AI and Vector capabilities.
    -   **Responsibilities**:
        -   Interface with LLMs (OpenAI, Azure OpenAI, HuggingFace, etc.) via `Senparc.AI.Kernel`.
        -   Manage AI Model configurations (`AIModel`).
        -   Manage Vector DB configurations (`AIVector`).
    -   **Key Files**: `Domain/Services/AIModelService.cs`, `Domain/Services/AIVectorService.cs`.

3.  **Senparc.Xncf.FileManager** (Source Module)
    -   **Role**: Manages physical/cloud files.
    -   **Responsibilities**:
        -   Store and retrieve raw files (PDF, Markdown, TXT, etc.).
        -   Provide file content streams to `KnowledgeBase`.

## Development Guidelines

### 1. NCF Patterns
-   **Services**: Inherit from `ServiceBase<TEntity>`. Use Dependency Injection for repositories and other services.
-   **Repositories**: Inherit from `RepositoryBase<TEntity>`.
-   **Entities**: Inherit from `EntityBase<TKey>`. Use `[Table]` attribute with `Register.DATABASE_PREFIX`.
-   **Registration**: Use `Register.cs` (inheriting `XncfRegisterBase`) to register services and configure the module.

### 2. RAG Implementation Strategy
-   **Embedding Flow**:
    1.  User uploads file -> `FileManager`.
    2.  `KnowledgeBase` reads file from `FileManager`.
    3.  `KnowledgeBase` splits text into chunks.
    4.  `KnowledgeBase` calls `AIKernel` to generate embeddings for chunks.
    5.  Embeddings are stored in the configured Vector DB (via `AIKernel` or direct integration).
    6.  `KnowledgeBasesDetail` stores the metadata and raw text.

-   **Query Flow**:
    1.  User submits query.
    2.  `KnowledgeBase` calls `AIKernel` to embed the query.
    3.  Search Vector DB for nearest neighbors.
    4.  Retrieve corresponding text from `KnowledgeBasesDetail` (or Vector DB payload).
    5.  Construct prompt with context.
    6.  Call `AIKernel` (Chat Completion) to generate answer.

### 3. Coding Conventions
-   **Async/Await**: Use asynchronous programming for all I/O and AI operations.
-   **DTOs**: Use DTOs for data transfer between layers (e.g., `KnowledgeBasesDto`).
-   **Mapping**: Use `AutoMapper` for Entity-DTO conversion.
-   **Error Handling**: Use `NcfExceptionBase` for domain exceptions.

### 4. Critical Commands
-   **Build & Run**: To avoid XncfBuilder Generated file conflicts, always use:
    ```bash
    rm -rf "/Volumes/DevelopAndData/SenparcProjects/NeuCharFramework/NcfPackageSources/src/Extensions/Senparc.Xncf.XncfBuilder/Senparc.Xncf.XncfBuilder/Generated" && cd tools/NcfSimulatedSite/Senparc.Web && dotnet run
    ```
-   **Migration**: When modifying entities, run `Add-Migration` in the `Senparc.Xncf.KnowledgeBase` project context (usually via Package Manager Console or dotnet ef tool).
    -   `dotnet ef migrations add [MigrationName] -c KnowledgeBaseSenparcEntities -p src/Extensions/Senparc.Xncf.KnowledgeBase/Senparc.Xncf.KnowledgeBase.csproj -s src/Extensions/Senparc.Xncf.KnowledgeBase/Senparc.Xncf.KnowledgeBase.csproj` (Adjust paths as needed).

## Immediate Tasks (RAG Upgrade)
1.  Implement **Text Splitting** logic in `KnowledgeBase`.
2.  Implement **Embedding Generation** integration with `AIKernel`.
3.  Implement **Vector Storage** logic.
4.  Implement **RAG Query** service.
