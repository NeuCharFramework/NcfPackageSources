# Senparc.Ncf.ImageUtility

`Senparc.Ncf.ImageUtility` provides small, filesystem-oriented image helpers used by legacy NCF components. The package is currently kept under the repository's unpublished sources and should be treated as compatibility infrastructure.

## Features

- Creates thumbnails from a `Stream` or an input file path.
- Supports fixed output dimensions, output directories, optional filename preservation, and optional source cleanup.
- Generates random file names for temporary image assets.

## Installation

```xml
<PackageReference Include="Senparc.Ncf.ImageUtility" Version="0.26.0-preview3" />
```

## Key API

- `ImageHelper.GetThumbnail(Stream, string, int, int, bool, string, bool, bool)` processes an image stream.
- `ImageHelper.GetThumbnail(string, int, int, bool, string, bool, bool)` processes an image file.
- `ImageHelper.GetRndFileName()` returns a random file name suitable for generated output.

Callers should validate image input and output paths before invoking the helpers. Do not use user-controlled paths without applying the host application's authorization and path-traversal rules.
