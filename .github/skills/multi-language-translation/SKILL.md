[中文版](SKILL.cn.md)

---
name: multi-language-translation
description: Create a Developer-Multi-Language branch and translate all Chinese comments in source files (.cs, .js, .css, etc.) to English, and translate README/documentation markdown files while preserving Chinese copies.
---

# Multi-Language Translation Workflow

This skill guides the complete process of creating the `Developer-Multi-Language` branch and translating all Chinese content in source code comments and documentation into English.

---

## Phase 0: Branch Setup

Before any translation work begins:

1. **Check the current branch** to confirm the base:
   ```bash
   git branch --show-current
   ```

2. **Create and switch to the new branch**:
   ```bash
   git checkout -b Developer-Multi-Language
   ```

3. **Verify the branch was created successfully**:
   ```bash
   git branch --show-current
   ```---

## Phase 1: Translate Source Code Comments

### Scope
- **Include**: All source code files: `.cs`, `.js`, `.ts`, `.css`, `.scss`, `.less`, `.html` (attribute values and template text are excluded; only comments and string literals used as developer-facing notes).
- **Exclude**: Configuration files (`.csproj`, `.json`, `.xml`, `.yaml`, `.yml`, `.config`, `.props`, `.targets`), binary files, and generated files (e.g., `obj/`, `bin/`, `Generated/` directories, `*.designer.cs`, `*.g.cs`).
- **Exclude**: UI display text (e.g., Razor `.cshtml` page visible text, `<label>`, `<button>` human-readable content), only translate code **comments** and **XML doc comments**.

### What counts as a "comment"
- Single-line comments: `// Chinese comments`
- Multi-line comments: `/* Chinese */`
- XML doc comments: `/// <summary>中文</summary>`
- CSS/SCSS/JS comments: `/* Chinese */`, `//中文`

### Translation Rules
- Translate comment content from Chinese to natural, idiomatic English.
- Preserve all code logic, formatting, indentation, and comment markers exactly.
- Do **not** alter any code, variable names, string values used at runtime, or file structure.
- Do **not** modify `.csproj` or any other config/project metadata files.
- If a comment is already in English (or another language), leave it unchanged.

### Workflow per file batch
1. Identify files containing Chinese characters using:```bash
   grep -rl '[^\x00-\x7F]' src/ --include="*.cs" --include="*.js" --include="*.ts" --include="*.css" --include="*.scss"
   ```
2. Work through files in logical groups (e.g., by module/folder).
3. For each file:
   - Read the file to understand context.
   - Translate all Chinese comments to English in-place.
   - Verify no code logic was changed.
4. After completing each module/folder group:
   - **Compile check** (for C# changes): `dotnet build` from the relevant project or solution root.
   - **Commit** with a descriptive message:
     ```
     git commit -m "translate: Chinese comments to English in [ModuleName]"
     ```

---

## Phase 2: Translate README and Documentation Files

### Scope
- **Include**: All `.md` files in the repository root, `docs/`, and any `README*.md` files in subdirectories.
- **Exclude**: Auto-generated changelogs, tool-generated files, and files already in English.

### Workflow per markdown file

For each `.md` file that contains Chinese content:

1. **Create a Chinese copy** (backup) by copying the original file with a `.cn.md` suffix:
   - Example: `README.md` → `README.cn.md`
   - Example: `docs/KnowledgeBase-Embedding-Implementation.md` → `docs/KnowledgeBase-Embedding-Implementation.cn.md`
   - The `.cn.md` file is the preserved Chinese version — **do not modify it**.

2. **Translate the source file** (the original, e.g., `README.md`) into English:
   - Translate all headings, paragraphs, list items, table content, and code block descriptions.
   - Preserve all Markdown formatting: headings (`#`), bold (`**`), code blocks (`` ` `` and ```` ``` ````), links, tables, etc.
   - Do **not** translate code samples inside fenced code blocks — only translate surrounding prose and inline comments within code blocks if they are in Chinese.
   - If the file is already entirely in English, skip it (do not create a `.cn.md`).

3. After completing each documentation group:
   - **Commit** with a descriptive message:
     ```
     git commit -m "translate: README/docs [filename] to English, preserve .cn.md"
     ```

---

## Phase 3: Final Verification and Cleanup

1. **Full build check** to ensure no compilation errors were introduced:
   ```bash
   dotnet build src/NcfPackageSources.sln
   ```
   Fix any compilation errors before proceeding.

2. **Verify no Chinese characters remain in source code comments**:
   ```bash
   grep -rn '[^\x00-\x7F]' src/ --include="*.cs" --include="*.js" --include="*.ts" --include="*.css"
   ```
   Review any remaining occurrences to confirm they are in-code string literals (acceptable) and not comments.

3. **Final commit** summarizing the overall change:
   ```bash
   git commit -m "feat: complete Chinese-to-English translation for Developer-Multi-Language branch"
   ```

---

## Commit Message Conventions

Use the following prefixes for all commits in this workflow:

| Prefix | Usage |
|---|---|
| `translate: ` | Source code comment translations |
| `translate: README/docs` | Documentation file translations |
| `feat: ` | Final summary or phase-completion commits |
| `fix: ` | Compilation fixes discovered during the process |

---

## Important Constraints

- **Never** modify `.csproj`, `.json`, `.xml`, `.yaml`, `.yml` configuration files for translation purposes.
- **Never** change runtime string values, enum names, method names, variable names, or any logic.
- **Never** remove the `.cn.md` Chinese backup files — they are permanent parallel files.
- **Always** commit after completing each module or document group rather than batching everything into one large commit.
- **Always** run a compilation check after translating any `.cs` files to catch accidental syntax corruption early.
- When a file is too large to translate in a single pass, split by logical sections (class, region, or function grouping) and commit after each section is complete.
