---
name: senparc-version-updater
description: Update version metadata for Senparc-prefixed .NET projects. Use when Codex needs to update a package version in .csproj files (preferring .net10+ variants), summarize project changes since the merge-base against master/main, add standardized Chinese changelog headers to changed .cs files, append PackageReleaseNotes entries, and recursively bump versions for projects that reference the updated project.
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
4. `.csproj` files mapped from changed `.cs` files (`changed_csprojs`) that must receive direct project updates.
5. Recursive dependent `.csproj` files that need passive version bumps.
6. Comparison baseline metadata (`master/main` branch ref and merge-base commit).

Special rule for generated files:

1. Never add header comments to `*.Generated.cs`.
2. If a `*.Generated.cs` change only updates timestamp-like text after regeneration and has no real content change, do not treat it as a changed file.

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
7. Keep one blank line between modification groups.

If the file already has the block, keep historical entries and append only one new modification group.
Always skip `*.Generated.cs` for header insertion.
`changed_cs_files` is generated from the repository-wide comparison window, so this step must not filter to the primary project directory.

### 4) Update `.csproj` For All Changed Projects

For all projects in `changed_csprojs` (including `primary_csproj`):

1. Ensure project version metadata and release notes are updated for this window.
2. For the same unmerged window (`comparison_base..HEAD`), version can only bump once per project.
3. If new commits are added before merge, merge details into the same version's release-note block instead of creating a new version entry.

### 5) Append `<PackageReleaseNotes>` In Selected `.csproj`

In the selected primary `.csproj` (must also satisfy Step 4):

1. Append new lines to existing `<PackageReleaseNotes>` content.
2. Keep the same leading spaces as the previous line.
3. Use this structure:
   - `[YYYY-MM-DD] vX.Y.Z <one-line summary>`
   - `1、<detail>`
   - `2、<detail>`
4. Include all key updates found in Step 2.
5. Within the same unmerged `comparison_base..HEAD` window, do not create another version bump entry; merge new details into the same `vX.Y.Z` release-note block.
6. `summary/detail` must describe project functional changes (feature, behavior, compatibility, bug fix, UI/UX, dependency adaptation).
7. Do not write process-tracking text in release notes, including but not limited to: `Repository baseline sync`, `sync window`, `comparison_base..HEAD`, or similar window/baseline markers.
8. `<PackageReleaseNotes>` must be located after `<Version>` in the `.csproj` file.
9. `<PackageReleaseNotes>` does not need to be immediately adjacent to `<Version>`. If it is already after `<Version>`, keep its current position (manual placement may exist).
10. If `<PackageReleaseNotes>` appears before `<Version>`, move `<PackageReleaseNotes>` to any valid location after `<Version>` while preserving indentation/content.
11. `<Version>` must be located after `<TargetFramework>` or `<TargetFrameworks>` in the `.csproj` file.
12. `<Version>` does not need to be immediately adjacent to `<TargetFramework>`/`<TargetFrameworks>`. If it is already after framework tags, keep its current position.
13. If `<Version>` appears before `<TargetFramework>`/`<TargetFrameworks>`, move `<Version>` to any valid location after framework tags while preserving indentation/content.

If `<PackageReleaseNotes>` does not exist, create it under the first `<PropertyGroup>` and match surrounding indentation.

### 6) Propagate Version Bumps To Referencing Projects

For all projects in `dependent_csprojs`:

1. Update version with passive bump policy (`patch` unless explicit override).
2. Append a passive release note item:
   - `[YYYY-MM-DD] vX.Y.Z Dependency update from <project-name> to <source-version>`
3. If a dependent project lacks `<Version>`, create it under the first `<PropertyGroup>` before writing release notes.
4. Continue recursively for projects referencing those projects.

Process in dependency layers from nearest dependents to farthest dependents, and avoid cycles with a visited set.

### 7) Validate Before Finish

Validate:

1. Every file in `changed_cs_files` contains a valid changelog block.
2. Every `.csproj` in `changed_csprojs` has updated version/release notes for current window.
3. All recursive dependents from scanner output are covered.
4. In every edited `.csproj`, `<Version>` is located after `<TargetFramework>` or `<TargetFrameworks>`.
5. In every edited `.csproj`, `<PackageReleaseNotes>` is located after `<Version>`.
6. In every edited `.csproj`, resulting `<Version>` is not lower than its pre-update `<Version>`.
7. All commits in `comparison_base..HEAD` are reflected by updated files/projects in this workflow.
8. Optional: run project build/tests before commit.

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

# 5) Inspect passive dependent projects
jq -r '.dependent_csprojs[]' /tmp/senparc-version-scan.json

# 6) Inspect commits included in this update window
jq -r '.commit_summaries[] | "\(.date) \(.commit) \(.message)"' /tmp/senparc-version-scan.json
```
