# Senparc.Xncf.OfficeExtension

`Senparc.Xncf.OfficeExtension` is an unpublished XNCF helper for reading Excel worksheets through EPPlus.

## Features

- Opens a worksheet from a file path, memory stream, or ASP.NET Core `IFormFile`.
- Converts a worksheet's used cells to tab-delimited text.
- Keeps the Excel dependency behind a small static helper surface.

## Installation

```xml
<PackageReference Include="Senparc.Xncf.OfficeExtension" Version="0.26.0-preview3" />
```

## Key API

- `EpPlusExtension.GetWorksheet(string)` opens the first worksheet in a file.
- `EpPlusExtension.GetWorksheet(MemoryStream)` opens the first worksheet in a stream.
- `EpPlusExtension.GetWorksheetAsync(IFormFile)` reads an uploaded workbook.
- `EpPlusExtension.ReadToString(ExcelWorksheet)` returns tab-delimited cell text.

The extension is unpublished and should be treated as compatibility code. Validate upload size and content, handle workbook disposal/ownership, and review the EPPlus license and deployment requirements for your use case.
