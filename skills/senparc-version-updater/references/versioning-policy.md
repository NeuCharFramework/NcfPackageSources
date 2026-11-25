# Versioning and Release Notes Policy

## Version Decision

Choose the bump level according to the project change:

1. `major`: breaking changes, incompatible public API removal, or incompatible behavior.
2. `minor`: new non-breaking capabilities, public API expansion, or feature enhancement.
3. `patch`: fixes and maintenance, including bug fixes, refactors, tests, documentation, and passive dependency updates.
4. Versions must be monotonic: the resulting `<Version>` must not be lower than the previous version.
5. Within one unmerged window, the version may remain unchanged while release notes are merged, but it must never move backwards.
6. All new changelog headers, release-note summaries and details, dependency notes, and other generated comments must be written in English.

## Comparison Window (`master`/`main`)

Version decisions and change summaries must use the comparison window against `master` or `main`:

1. Baseline priority: `origin/master` -> `origin/main` -> `master` -> `main`.
2. Use `merge-base(HEAD, baseline)` as `comparison_base`.
3. The update scope is every commit in `comparison_base..HEAD` plus current uncommitted changes. Changed files are the union of paths touched by every commit, not only the final net diff from `git diff comparison_base..HEAD`.
3.1. Scan the entire repository; do not restrict impact discovery to the selected project directory.
3.2. Files changed and later restored within the window still belong to the touched-file set and must map to their projects for functional release-note coverage.
3.3. Deleted `.cs` files and non-timestamp-only `*.Generated.cs` files do not receive headers, but their containing projects must still be included in `changed_csprojs`.
3.4. Auto-generated migration files under `Domain/Migrations/<database-name>/` do not receive headers. They remain in change monitoring, their containing non-test projects must be included in `changed_csprojs`, and their functional changes must be recorded in project version metadata and `<PackageReleaseNotes>`.
4. If `master`/`main` cannot be resolved or merge-base calculation fails, stop with an error. Never fall back to a "last changed csproj" window.

## Unmerged Branch Rules

For commits not yet merged into `master`/`main` (the same `comparison_base..HEAD` window):

1. Bump a project version at most once.
2. Do not create another version bump for later commits in the same window.
3. Merge later changes into the same version's `<PackageReleaseNotes>` block.
4. Only after the branch is merged and a new comparison window begins should another `major`/`minor`/`patch` decision be made.

## Affected Project Coverage

1. Any file under a project path touched by the commit union, including `.cs`, `.cshtml`, `.js`, `.resx`, and `.axaml`, maps back to its `.csproj`; directly changed `.csproj` files are included as well.
2. For every changed `.props`, resolve explicit `<Import Project="..." />` paths, including `$(MSBuildThisFileDirectory)` and `$(MSBuildProjectDirectory)`, and recursively follow `.props` imports.
3. Every non-test project that directly or indirectly imports a changed `.props` must be included in `changed_csprojs`, even when none of its `.cs` files changed.
4. Every project in `changed_csprojs` must update its own `.csproj` version and release notes.
5. Do not update only the selected primary project while ignoring projects containing other changed code or importing changed `.props`.
6. If any `.cs` file is edited during this run, including a header-only edit, its mapped project must merge/update the current-window version block in `<PackageReleaseNotes>`.
7. A `.props` file does not receive a C# header; its dependency, compatibility, or build behavior impact must be described functionally in every importing project's release notes.
8. Test projects, including `*.Tests.csproj`, `*Tests.csproj`, and projects under `Tests`, `*.Tests`, or `*Tests` directories, are not released. Do not add, update, or remove their `<Version>`, and do not require new package release notes. Existing notes may be reordered as complete blocks when needed.
9. Test projects are excluded from version updates but must still be traversed when discovering recursive non-test dependents.

## Prerelease Handling

When the current version contains a `-preview.N` suffix:

1. Calculate the base `major`/`minor`/`patch` bump first.
2. Preserve the preview channel and increment `N` (starting at `1` when no number exists).

Examples:

1. `1.4.2-preview.3` + `patch` -> `1.4.3-preview.4`
2. `1.4.2-preview.3` + `minor` -> `1.5.0-preview.4`
3. `1.4.2` + `patch` -> `1.4.3`

## Appending `<PackageReleaseNotes>`

Append entries in chronological order, normally at the end, preserving the surrounding indentation:

```xml
[YYYY-MM-DD] vX.Y.Z <one-line summary>
1. <detail one>
2. <detail two>
```

Requirements:

1. Describe functional project changes: new capabilities, behavior changes, compatibility work, fixes, UX improvements, or dependency impact.
2. Do not write process-tracking or window-identification text such as `Repository baseline sync`, `sync window`, or `comparison_base..HEAD`.
3. Multiple commits in one unmerged window may be merged into one functional description; do not describe the synchronization process itself.
4. Write every new summary and detail in English, including passive dependency notes.
5. `<PackageReleaseNotes>` must appear after `<Version>`.
5. It does not need to be adjacent to `<Version>`; if already after it, keep the existing position.
6. If it appears before `<Version>`, move it after `<Version>` while preserving content and indentation.
7. `<Version>` must appear after `<TargetFramework>` or `<TargetFrameworks>`.
8. `<Version>` does not need to be adjacent to the framework tags; if already after them, keep the existing position.
9. If `<Version>` appears before the framework tags, move it after them while preserving content and indentation.
10. Do not leave blank lines inside `<PackageReleaseNotes>`, including between entries and detail lines.
11. New release-note lines must use the same indentation as the previous content line.
12. If the version remains unchanged within one unmerged window, merge all new project changes into the current version block; do not skip them.
13. Check and correct dated release-note order from oldest to newest, moving each entry together with all detail lines.
14. Preserve the existing relative order of entries with the same date when sequence is unknown; append a new same-date entry after existing entries for that date.
15. Preserve undated legacy entries and their relative order. Do not invent dates; report entries whose chronological position cannot be established.
16. When merging into the current version block, use the most recent actual change date represented by the block and move the complete block to the correct position.

For passive dependency updates, use:

```xml
[YYYY-MM-DD] vX.Y.Z Dependency update from <ProjectName> to <SourceVersion>
```

## Recursive Dependency Updates

1. All direct changed projects (projects containing changed `.cs` files plus changed `.props` importers) use their actual `major`/`minor`/`patch` decision.
2. Passive dependent projects use a `patch` bump by default.
3. If a passive dependent project lacks `<Version>`, create it before writing release notes.
4. Continue recursively when a dependent project is itself referenced by another project until the chain ends.
5. Use a visited set to avoid cycles.
6. Start recursive discovery from the union of all direct changed projects, not only from an unrelated command-line primary project.
