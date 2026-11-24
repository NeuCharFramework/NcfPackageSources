---
name: add-modify-log
description: Add standardized C# file header changelog blocks. Superseded in practice by senparc-version-updater; use this entry only when the user names add-modify-log explicitly.
---

# Add Modify Log

This skill is a **thin alias** for the header step in [senparc-version-updater](../senparc-version-updater/SKILL.md).

Do not maintain a second header format here. For any real version or release work, run `senparc-version-updater` first and follow [header-template.md](../senparc-version-updater/references/header-template.md).

When only a header append is needed on a few files:

1. Use the exact block format in `header-template.md`.
2. Append one `Modified by` / `Change description` pair per change window.
3. Skip `*.Generated.cs`, unit-test projects, `MultipleDatabase/`, and EF files under `Domain/Migrations/<DatabaseName>/` (same skip rules as `senparc-version-updater`).