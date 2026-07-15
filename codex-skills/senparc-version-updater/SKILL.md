---
name: senparc-version-updater
description: Update version metadata for Senparc-prefixed .NET projects. Use when Codex needs to update package versions in .csproj files (preferring .net10+ variants), propagate changed shared .props imports to all importing projects, summarize project changes since the merge-base against master/main, add standardized Chinese changelog headers to changed .cs files, append PackageReleaseNotes entries, and recursively bump versions for referencing projects.
---

# Senparc Version Updater

## Inputs

Provide:

1. Repository root path.
2. Target project path (a `.csproj` file or the project directory).
3. Author name for change records (default `Senparc`).

## Run Scanner First

Run:

```bash
python3 scripts/senparc_version_scan.py --root <repo-root> --project <project-path>
```

Use the output JSON as the source of truth for:

1. The selected primary `.csproj` to edit.
2. Changed files in the current update scope (`comparison_base..HEAD` plus uncommitted) across the entire repository, not just the primary project directory.
3. Changed `.cs` files that must receive header updates (repository-wide).
4. Changed `.props` files and all projects that import them directly or through another `.props` file (`changed_props_importers`).
5. `.csproj` files mapped from changed `.cs` files or changed `.props` imports (`changed_csprojs`) that must receive direct project updates.
6. Direct update roots (`dependency_roots`) and recursive dependent `.csproj` files that need passive version bumps.
7. Comparison baseline metadata (`master/main` branch ref and merge-base commit).

Special skip rules for header updates:

1. Never add header comments to `*.Generated.cs`.
2. If a `*.Generated.cs` change only updates timestamp-like text after regeneration and has no real content change, do not treat it as a changed file.
3. Never add header comments to files under unit test projects (directory names such as `Tests`, `*.Tests`, or `*Tests`).
4. Never add header comments to files under `MultipleDatabase/` directory.

## Commit Baseline Rule (Strict)

Before any version decision, resolve comparison baseline in this priority:

1. `origin/master`
2. `origin/main`
3. `master`
4. `main`

Then:

1. Compute `merge-base(HEAD, baseline-branch)` as `comparison_base`.
2. Treat all commits in `comparison_base..HEAD` as this update's commit set.
3. Include uncommitted workspace changes in the same update scope.
4. If no `master/main` branch can be resolved or merge-base fails, stop and report error (do not fallback to other windows).

## Apply Workflow

### 1) Select The `.csproj` To Edit

Follow this selection rule per project directory:

1. If multiple `.csproj` files exist, select `.net10.csproj` or the highest `.netXX.csproj` above 10.
2. If no filename-level `.net10+` candidate exists, select the file with the highest `TargetFramework` major version.
3. If only one `.csproj` exists, use it directly.

Use the scanner's `primary_csproj` and `dependent_csprojs` to avoid inconsistent manual selection.

### 2) Derive New Version From Real Changes

Treat the time window as:

1. Start: scanner output `comparison_base.commit` (merge-base vs `master/main`).
2. End: current workspace state (`HEAD` plus uncommitted files).

Summarize all project updates in this window and decide bump type:

1. `major`: breaking API/behavior or incompatible contract change.
2. `minor`: new non-breaking features/public capability expansion.
3. `patch`: bug fix/refactor/test/docs/passive dependency bump.

If current version contains prerelease suffix (`-preview.N`), preserve prerelease channel and increment its number while applying the major/minor/patch base bump.
For commits that are still unmerged into `master/main` (same `comparison_base..HEAD` window), bump version at most once. Subsequent updates in the same unmerged window must reuse the same version and only append/merge release notes.
Version must be monotonic non-decreasing per project: never lower `<Version>` than the pre-update value. In the same unmerged window, reusing the same version is allowed when only merging release notes.

See [versioning-policy.md](references/versioning-policy.md).

### 3) Update Header Logs In All Changed `.cs` Files

For every file in `changed_cs_files`:

1. Ensure a top-of-file changelog block exists.
2. Use the exact format in [header-template.md](references/header-template.md).
3. Replace `文件名` with actual filename.
4. Set `文件功能描述` once and keep it stable afterward.
5. Set `创建标识` date to file creation date (`creation_date_yyyymmdd` from scanner, fallback to today).
6. Append one new pair for current change:
   - `修改标识：<author> - YYYYMMDD`
   - `修改描述：v<new-version> <summary>`
7. Keep exactly one blank line between modification groups (no extra blank lines).
8. Skip files matched by the special skip rules above (`*.Generated.cs`, unit test project files, and `MultipleDatabase/` directory files).

If the file already has the block, keep historical entries and append only one new modification group.
Always skip `*.Generated.cs` for header insertion.
`changed_cs_files` is generated from the repository-wide comparison window, so this step must not filter to the primary project directory.

### 4) Resolve Changed `.props` Imports And Update All Changed Projects

For every changed `.props` file:

1. Resolve explicit `<Import Project="...props" />` paths, including `$(MSBuildThisFileDirectory)` and `$(MSBuildProjectDirectory)`.
2. Follow `.props` -> `.props` imports transitively until all importing `.csproj` files are found.
3. Treat every importing project as directly changed, even if none of its own `.cs` files changed.
4. Add these projects to `changed_csprojs`, `dependency_roots`, and `changed_csproj_to_props_files`.
5. A changed `.props` file does not receive a C# header block; its functional effect must be recorded in every importing project's `<PackageReleaseNotes>`.

For all projects in `changed_csprojs` (from changed `.cs` files or changed `.props` imports):

1. Ensure project version metadata and release notes are updated for this window.
2. For the same unmerged window (`comparison_base..HEAD`), version can only bump once per project.
3. If new commits are added before merge, merge details into the same version's release-note block instead of creating a new version entry.
4. If any `.cs` file is edited in this run (including header-only edits), its mapped project in `changed_csprojs` must receive a release-note merge/update in the current version block (no silent skip).
5. If a shared `.props` file changes package/version properties, describe the resulting dependency or compatibility change functionally in each importing project; do not use generic process text.

### 5) Append `<PackageReleaseNotes>` In Selected `.csproj`

In every `.csproj` in `changed_csprojs` (the selected primary must also satisfy Step 4 when affected):

1. Append new lines to existing `<PackageReleaseNotes>` content.
2. Keep the same leading spaces as the previous line.
3. Do not insert blank lines inside `<PackageReleaseNotes>` (including between entries and detail lines).
4. Use this structure:
   - `[YYYY-MM-DD] vX.Y.Z <one-line summary>`
   - `1、<detail>`
   - `2、<detail>`
5. Include all key updates found in Step 2.
6. Within the same unmerged `comparison_base..HEAD` window, do not create another version bump entry; merge new details into the same `vX.Y.Z` release-note block.
7. `summary/detail` must describe project functional changes (feature, behavior, compatibility, bug fix, UI/UX, dependency adaptation).
8. Do not write process-tracking text in release notes, including but not limited to: `Repository baseline sync`, `sync window`, `comparison_base..HEAD`, or similar window/baseline markers.
9. `<PackageReleaseNotes>` must be located after `<Version>` in the `.csproj` file.
10. `<PackageReleaseNotes>` does not need to be immediately adjacent to `<Version>`. If it is already after `<Version>`, keep its current position (manual placement may exist).
11. If `<PackageReleaseNotes>` appears before `<Version>`, move `<PackageReleaseNotes>` to any valid location after `<Version>` while preserving indentation/content.
12. `<Version>` must be located after `<TargetFramework>` or `<TargetFrameworks>` in the `.csproj` file.
13. `<Version>` does not need to be immediately adjacent to `<TargetFramework>`/`<TargetFrameworks>`. If it is already after framework tags, keep its current position.
14. If `<Version>` appears before `<TargetFramework>`/`<TargetFrameworks>`, move `<Version>` to any valid location after framework tags while preserving indentation/content.

If `<PackageReleaseNotes>` does not exist, create it under the first `<PropertyGroup>` and match surrounding indentation.

### 6) Propagate Version Bumps To Referencing Projects

For all projects in `dependent_csprojs`, starting from every project in `dependency_roots`:

1. Update version with passive bump policy (`patch` unless explicit override).
2. Append a passive release note item:
   - `[YYYY-MM-DD] vX.Y.Z Dependency update from <project-name> to <source-version>`
3. If a dependent project lacks `<Version>`, create it under the first `<PropertyGroup>` before writing release notes.
4. Continue recursively for projects referencing those projects.

Process the union of dependency layers from all direct roots, from nearest dependents to farthest dependents, and avoid cycles with a visited set. Do not calculate dependents from an unrelated `primary_csproj` when other projects are the actual changed roots.

### 7) Validate Before Finish

Validate:

1. Every eligible file in `changed_cs_files` (after applying special skip rules) contains a valid changelog block.
2. Every changed `.props` file is mapped to all direct and transitive importing projects in `changed_props_importers`.
3. Every `.csproj` in `changed_csprojs` (including `.props` importers) has updated version/release notes for current window.
4. All recursive dependents from every `dependency_roots` project are covered.
5. In every edited `.csproj`, `<Version>` is located after `<TargetFramework>` or `<TargetFrameworks>`.
6. In every edited `.csproj`, `<PackageReleaseNotes>` is located after `<Version>`.
7. In every edited `.csproj`, lines appended to `<PackageReleaseNotes>` use the same indentation as the previous note line.
8. In every edited `.csproj`, `<PackageReleaseNotes>` contains no blank line.
9. In every edited `.csproj`, resulting `<Version>` is not lower than its pre-update `<Version>`.
10. All commits in `comparison_base..HEAD` are reflected by updated files/projects in this workflow.
11. For every edited `.cs` file in this run, its mapped `.csproj` has a release-note merge/update in current window version block.
12. Optional: run project build/tests before commit.

## Suggested Command Sequence

```bash
# 1) Scan baseline and impact scope
python3 scripts/senparc_version_scan.py --root <repo-root> --project <project-path> > /tmp/senparc-version-scan.json

# 2) Inspect merge-base baseline
jq -r '.comparison_base.branch_ref, .comparison_base.commit, .comparison_range' /tmp/senparc-version-scan.json

# 3) Inspect impacted C# files
jq -r '.changed_cs_files[].path' /tmp/senparc-version-scan.json

# 3.1) Optional: inspect only files under selected primary project directory
jq -r '.changed_cs_files_in_primary_project[].path' /tmp/senparc-version-scan.json

# 4) Inspect direct changed projects (.cs -> .csproj mapping)
jq -r '.changed_csprojs[]' /tmp/senparc-version-scan.json

# 4.1) Inspect mapping from changed .cs files to target .csproj
jq -r '.changed_csproj_to_cs_files | to_entries[] | .key as $p | .value[] | "\($p)\t\(.path)"' /tmp/senparc-version-scan.json

# 4.2) Inspect changed .props files and all importing projects
jq -r '.changed_props_importers | to_entries[] | .key as $props | .value[] | "\($props)\t\(.)"' /tmp/senparc-version-scan.json

# 4.3) Inspect direct dependency roots (.cs changes + .props importers)
jq -r '.dependency_roots[]' /tmp/senparc-version-scan.json

# 5) Inspect passive dependent projects
jq -r '.dependent_csprojs[]' /tmp/senparc-version-scan.json

# 6) Inspect commits included in this update window
jq -r '.commit_summaries[] | "\(.date) \(.commit) \(.message)"' /tmp/senparc-version-scan.json
```
