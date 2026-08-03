# Senparc.Xncf.FileManager

`Senparc.Xncf.FileManager` is an NCF module for bounded local file storage, folders, metadata, downloads, and safe text extraction for KnowledgeBase.

## Features

- Stores physical files under `App_Data/NcfFiles/yyyy/MM` with generated storage names and validated relative-path metadata.
- Limits uploads to 50 MB per file, 20 files and 100 MB per request, and an explicit extension allowlist.
- Cleans up the physical file when metadata persistence fails and stages deletion so a database failure can restore the file.
- Prevents deletion of non-empty folders and rejects missing parents, duplicate sibling names, and path-like folder names.
- Returns the original filename for downloads and exposes `GetExtractedTextAsync` for KnowledgeBase ingestion.
- Safely decodes UTF-8/UTF-16 text and parses DOCX, PPTX, and XLSX XML with DTDs disabled and decompressed-entry limits.
- Protects file listing and deletion application-service APIs with the NCF `AdminOnly` policy.

## Installation

```xml
<PackageReference Include="Senparc.Xncf.FileManager" Version="0.4.0-preview4" />
```

## Key API

- `Register` activates the module.
- `NcfFolderService` creates, validates, queries, updates, and safely deletes folder records.
- `NcfFileService` manages bounded physical storage and metadata lifecycle.
- `NcfFileTextExtractor` provides dependency-free extraction for `.txt`, `.log`, `.md`, `.markdown`, `.csv`, `.tsv`, `.json`, `.xml`, `.yaml`, `.yml`, `.html`, `.htm`, `.css`, `.js`, `.ts`, `.cs`, `.sql`, `.docx`, `.pptx`, and `.xlsx`.
- `FileTemplateAppService` and `FileTemplate_GetListResponse` expose template listing.
- `CreateFolderRequest`, `FileUploadModel`, and `DeleteFileRequest` model common operations.

## Current boundaries

- Upload support is broader than KnowledgeBase extraction support. PDF, legacy `.doc/.xls/.ppt`, images, archives, OCR, and audio require a separately sandboxed parser and are rejected by `GetExtractedTextAsync` with an explicit error.
- The module does not perform antivirus/content-disarm scanning or object-storage replication. Production hosts should scan uploads before exposing them to other users.
- KnowledgeBase ingestion is snapshot-based; FileManager does not depend on or cascade-delete data in higher-level modules.
