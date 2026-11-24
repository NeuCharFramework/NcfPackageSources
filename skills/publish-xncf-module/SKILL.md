---
name: publish-xncf-module
description: Align an XNCF Register version with its NuGet package version, changelog headers, and PackageReleaseNotes, then pack a Release nupkg. Use when the user asks to version, pack, or publish an XNCF. Do not run dotnet nuget push unless the user explicitly asks.
---

# Publish an XNCF

Version numbers, C# file headers, and `<PackageReleaseNotes>` follow [senparc-version-updater](../senparc-version-updater/SKILL.md). When that skill is available in the workspace, run its scan script before editing versions. If not, still keep `Register.Version` identical to the csproj `<Version>` and append release notes in the same format. Details: [publish.md](../develop-xncf-modules/references/publish.md).

## Pre-publish checklist

- `Register.Version` matches csproj `<Version>`.
- New modules start release history from their genesis entry, not the template's `v1.1.x` history.
- `Uid` and `DATABASE_PREFIX` match the last published package.
- The module csproj packs under `Release`. Do not leave `OutputPath` pointing outside the site to non-existent folders.
- Only modules in the NCF package source repo may enter the pipeline that pushes on merge to `master`. Modules in ordinary sites are not on that pipeline.

## Pack

When the user asks to pack:

```bash
dotnet pack <module.csproj> -c Release
```

Verify the nupkg contains the expected assemblies, resources, and `icon`. The template package is a separate project; see [template-maintenance.md](../develop-xncf-modules/references/template-maintenance.md). Do not mix module and template package versions.

## Push

Run local `dotnet nuget push` only when the user explicitly requests it. Without that request, packing and version alignment are the deliverable.

Do not treat a successful source build as proof of package contents, or a generated package as proof that nuget.org accepted it.