# Repository Agent Skills

All Cursor/Codex skills for this repository live under `skills/`. Skill bodies and `description` front matter are **English**. References and scripts may sit beside each skill in the same folder.

## Catalog

| Skill | Purpose |
| --- | --- |
| [create-xncf-module](create-xncf-module/SKILL.md) | Scaffold and integrate a new XNCF from the NuGet template |
| [develop-xncf-modules](develop-xncf-modules/SKILL.md) | Route XNCF work (create, maintain, publish, templates) |
| [maintain-xncf-module](maintain-xncf-module/SKILL.md) | Extend an existing XNCF (domain, UI, migrations, tests) |
| [publish-xncf-module](publish-xncf-module/SKILL.md) | Version, pack, and release notes for an XNCF |
| [senparc-version-updater](senparc-version-updater/SKILL.md) | Bump package versions, `PackageReleaseNotes`, C# headers from merge-base |
| [upgrade-senparc-packages](upgrade-senparc-packages/SKILL.md) | Upgrade **referenced** `Senparc.*` NuGet packages in scoped projects |
| [multi-language-translation](multi-language-translation/SKILL.md) | Source/doc comment translation branch workflow (not runtime `.resx`) |
| [add-modify-log](add-modify-log/SKILL.md) | Alias → file headers (use `senparc-version-updater` for full workflow) |

## Duplicates removed / consolidated

| Former location | Resolution |
| --- | --- |
| `codex-skills/senparc-version-updater/` | Moved to `skills/senparc-version-updater/`; old path is a redirect README only |
| `.github/skills/*` | Moved or merged here; `.github/skills/README.md` points to this catalog |
| `upgrade-senparc-packages/` (repo root, gitignored) | Canonical copy under `skills/upgrade-senparc-packages/` |
| `.github/skills/update-project-version` | **Removed** — simple `+1` build bumps and notes are covered by `senparc-version-updater` |

## Not duplicates

- **`senparc-version-updater`** updates **this repo's package version metadata** and changelogs.
- **`upgrade-senparc-packages`** updates **NuGet PackageReference versions** of Senparc dependencies in a target solution or folder.

Use the skill that matches the user's ask; do not run both unless both dependency bumps and package release metadata are required.

## IDE-local copies

- `.cursor/skills/*` (gitignored) may contain thin stubs that point at files under `skills/`. Treat `skills/` as canonical; do not maintain a second full copy there.
- `.claude/skills/playwright-cli` is tool-specific and lives outside this catalog unless moved here intentionally.