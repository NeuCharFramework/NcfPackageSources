# Senparc.Xncf.FileManager

`Senparc.Xncf.FileManager` is an NCF module for bounded local file storage that serves two isolated purposes: safe source documents for KnowledgeBase and publishable static assets for the site.

## Features

- Separates `KnowledgeBase` and `SiteAsset` records and folders. A file cannot be changed from one purpose to the other in the management UI.
- Stores new physical files below `App_Data/NcfFiles/knowledge-base/yyyy/MM` or `App_Data/NcfFiles/site-assets/yyyy/MM` with generated storage names and validated relative-path metadata. Legacy `yyyy/MM` knowledge-base paths remain readable.
- Limits uploads to 50 MB per file, 20 files and 100 MB per request, with a purpose-specific extension allowlist.
- Computes SHA-256 while writing each new file and persists its MIME type and content hash.
- Keeps static assets private by default. Only a published `SiteAsset` is available anonymously at `/assets/{id}/{fingerprint}`; the endpoint resolves metadata rather than accepting a file-system path, sends `nosniff`, supports ranges, and uses immutable caching only for a matching fingerprint.
- Cleans up the physical file when metadata persistence fails and stages deletion so a database failure can restore the file.
- Prevents deletion of non-empty folders and rejects missing parents, duplicate sibling names, and path-like folder names.
- Returns the original filename for downloads and exposes `GetExtractedTextAsync` only for `KnowledgeBase` sources.
- Safely decodes UTF-8/UTF-16 text and parses DOCX, PPTX, and XLSX XML with DTDs disabled and decompressed-entry limits.
- Protects file listing and deletion application-service APIs with the NCF `AdminOnly` policy.

## Installation

```xml
<PackageReference Include="Senparc.Xncf.FileManager" Version="0.4.0-preview4" />
```

## Key API

- `Register` activates the module.
- `NcfFolderService` creates, validates, queries, updates, and safely deletes folder records.
- `NcfFileService` manages bounded physical storage, resource-purpose/visibility transitions, content metadata, and public asset URLs.
- `FileAssetController` serves explicitly published site assets through `/assets/{id}/{fingerprint}`.
- `NcfFileTextExtractor` provides dependency-free extraction for `.txt`, `.log`, `.md`, `.markdown`, `.csv`, `.tsv`, `.json`, `.xml`, `.yaml`, `.yml`, `.html`, `.htm`, `.css`, `.js`, `.ts`, `.cs`, `.sql`, `.docx`, `.pptx`, and `.xlsx`.
- `FileTemplateAppService` and `FileTemplate_GetListResponse` expose template listing.
- `CreateFolderRequest`, `FileUploadModel`, and `DeleteFileRequest` model common operations.

## Current boundaries

- KnowledgeBase uploads are intentionally limited to formats the module can extract safely: text, structured/code text, `.docx`, `.xlsx`, and `.pptx`. PDF, legacy Office, images, OCR, archives, and audio require a separately sandboxed parser.
- Static-asset uploads accept images, audio/video, and fonts. HTML, SVG, JavaScript, and archives are deliberately excluded from the same-origin public-asset endpoint.
- The module does not perform antivirus/content-disarm scanning or object-storage replication. Production hosts should scan uploads before exposing them to other users.
- KnowledgeBase ingestion is snapshot-based; FileManager does not depend on or cascade-delete data in higher-level modules.
