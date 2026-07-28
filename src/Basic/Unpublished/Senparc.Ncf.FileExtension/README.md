# Senparc.Ncf.FileExtension

`Senparc.Ncf.FileExtension` provides a small ASP.NET Core upload helper. The source is maintained under the unpublished NCF extensions area and is intended for compatibility with existing applications.

## Features

- Asynchronously copies an `IFormFile` to a specified output path.
- Creates a target directory when necessary.
- Supports both an explicit full output path and a directory-plus-file-name overload.

## Installation

```xml
<PackageReference Include="Senparc.Ncf.FileExtension" Version="0.26.0-preview3" />
```

## Key API

- `FileExtension.Upload(IFormFile formFile, string outPath)` writes to an explicit path.
- `FileExtension.Upload(IFormFile formFile, string fileName, string paths)` combines a directory and file name.

The helper throws when the form file is null or empty. Validate file size, extension, content type, destination authorization, and path traversal in the host before calling it; do not use the original client filename as an unrestricted path component.
