# Senparc.Xncf.FileManager

`Senparc.Xncf.FileManager` is an NCF module for managing folders, files, upload records, and reusable file templates.

## Features

- Persists `NcfFolder` and `NcfFile` records with DTO and service layers.
- Provides file upload, deletion, listing, and folder creation request models.
- Includes `FileTemplateAppService` for reusable file-template metadata.
- Uses NCF's multi-database context and module resource conventions.

## Installation

```xml
<PackageReference Include="Senparc.Xncf.FileManager" Version="0.26.0-preview3" />
```

## Key API

- `Register` activates the module.
- `NcfFolderService` creates and queries folder records.
- `NcfFileService` manages file metadata and file records.
- `FileTemplateAppService` and `FileTemplate_GetListResponse` expose template listing.
- `CreateFolderRequest`, `FileUploadModel`, and `DeleteFileRequest` model common operations.

The module stores metadata; the host must define physical storage, authorization, size/type limits, virus scanning, and path-traversal protection. Never trust a client-supplied path or filename.
