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
2. Changed files in the current update scope (`comparison_base..HEAD` plus uncommitted).
3. Changed `.cs` files that must receive header updates.
4. Recursive dependent `.csproj` files that need passive version bumps.
5. Comparison baseline metadata (`master/main` branch ref and merge-base commit).

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

### 4) Append `<PackageReleaseNotes>` In Selected `.csproj`

In the selected primary `.csproj`:

1. Append new lines to existing `<PackageReleaseNotes>` content.
2. Keep the same leading spaces as the previous line.
3. Use this structure:
   - `[YYYY-MM-DD] vX.Y.Z <one-line summary>`
   - `1、<detail>`
   - `2、<detail>`
4. Include all key updates found in Step 2.

If `<PackageReleaseNotes>` does not exist, create it under the first `<PropertyGroup>` and match surrounding indentation.

### 5) Propagate Version Bumps To Referencing Projects

For all projects in `dependent_csprojs`:

1. Update version with passive bump policy (`patch` unless explicit override).
2. Append a passive release note item:
   - `[YYYY-MM-DD] vX.Y.Z Dependency update from <project-name> to <source-version>`
3. If a dependent project lacks `<Version>`, create it under the first `<PropertyGroup>` before writing release notes.
4. Continue recursively for projects referencing those projects.

Process in dependency layers from nearest dependents to farthest dependents, and avoid cycles with a visited set.

### 6) Validate Before Finish

Validate:

1. Every file in `changed_cs_files` contains a valid changelog block.
2. Every edited `.csproj` has incremented version and appended release note entry.
3. All recursive dependents from scanner output are covered.
4. All commits in `comparison_base..HEAD` are reflected by updated files/projects in this workflow.
5. Optional: run project build/tests before commit.

## Suggested Command Sequence

```bash
# 1) Scan baseline and impact scope
python3 scripts/senparc_version_scan.py --root <repo-root> --project <project-path> > /tmp/senparc-version-scan.json

# 2) Inspect merge-base baseline
jq -r '.comparison_base.branch_ref, .comparison_base.commit, .comparison_range' /tmp/senparc-version-scan.json

# 3) Inspect impacted C# files
jq -r '.changed_cs_files[].path' /tmp/senparc-version-scan.json

# 4) Inspect passive dependent projects
jq -r '.dependent_csprojs[]' /tmp/senparc-version-scan.json

# 5) Inspect commits included in this update window
jq -r '.commit_summaries[] | "\(.date) \(.commit) \(.message)"' /tmp/senparc-version-scan.json
```
