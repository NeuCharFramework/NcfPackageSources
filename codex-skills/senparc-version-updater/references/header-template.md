# C# File Header Template

Use the following format at the top of a target file:

```csharp
/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: <actual file name>
    File description: <one-sentence description of the file responsibility>

    Created by: <author> - <YYYYMMDD>

    Modified by: <author> - <YYYYMMDD>
    Change description: v<version> <summary of this change>

----------------------------------------------------------------*/
```

## Field Rules

1. `File name` must always match the current file name.
2. Set `File description` once and keep it stable; do not overwrite it in later changes.
3. Prefer the Git first-commit date for `Created by`, formatted as `YYYYMMDD`.
4. `Modified by` and `Change description` must always appear as a pair.
5. Keep exactly one blank line between modification groups.
6. The default author is `Senparc` unless the user explicitly provides another author.
7. Never add this header block to `*.Generated.cs` files.
8. If a `*.Generated.cs` file only changes timestamp-like text after regeneration and has no substantive content change, do not treat it as a changed file.
9. Do not add this header block to auto-generated migration files under `Domain/Migrations/<database-name>/` (migration classes, `*.Designer.cs`, and `*ModelSnapshot.cs`) because regeneration can overwrite them. These files remain in the change scope; record their functional changes in the containing non-test project's `<PackageReleaseNotes>`.

## Append Rules

1. If the file already has this header block, append only a new `Modified by` and `Change description` group.
2. If the file has no header block, insert the complete template at the top and then add the current modification group.
