#!/usr/bin/env python3
"""Apply senparc-version-updater changes from scan JSON (headers, csproj versions, notes)."""
from __future__ import annotations

import argparse
import json
import re
import subprocess
import sys
from collections import defaultdict
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path


HEADER_END = "----------------------------------------------------------------*/"
BLOCK_START = "/*----------------------------------------------------------------"
TEST_DIR_MARKERS = ("/Tests/", ".Tests/", "/Senparc.Ncf.XncfBase.Tests/")
MULTIPLE_DB = "/MultipleDatabase/"
MIGRATION_RE = re.compile(r"(?:^|/)Domain/Migrations/[^/]+/", re.I)
VERSION_TAG_RE = re.compile(r"(<Version>\s*)([^<]+)(\s*</Version>)", re.I)
NOTES_RE = re.compile(r"(<PackageReleaseNotes>)(.*?)(</PackageReleaseNotes>)", re.S | re.I)
WINDOW_NOTE_RE = re.compile(r"\[2026-11-2[67]\]")
PREVIEW_RE = re.compile(
    r"^(?P<base>\d+(?:\.\d+)*)(?:-(?P<channel>preview)\.?(?P<num>\d+))?(?:\+[\w.]*)?$",
    re.I,
)


@dataclass
class ProjectPlan:
    path: str
    current_version: str
    new_version: str
    bump: str
    summary: str
    details: list[str]
    skip_version: bool  # already has complete window notes + version


def run_git(root: Path, args: list[str]) -> str:
    proc = subprocess.run(
        ["git", "-C", str(root), *args],
        capture_output=True,
        text=True,
        check=False,
    )
    return proc.stdout


def parse_version_parts(version: str) -> tuple[list[int], bool, int | None]:
    preview_num: int | None = None
    dotted_preview = False
    raw = version.strip()
    m = PREVIEW_RE.match(raw)
    base = raw
    if m:
        base = m.group("base")
        if m.group("num") is not None:
            preview_num = int(m.group("num"))
            dotted_preview = f"-preview.{preview_num}" in raw
    parts: list[int] = []
    for seg in base.split("."):
        try:
            parts.append(int(seg))
        except ValueError:
            parts.append(0)
    return parts, dotted_preview, preview_num


def format_version(parts: list[int], dotted_preview: bool, preview_num: int | None) -> str:
    out = ".".join(str(p) for p in parts)
    if preview_num is not None:
        out += f"-preview.{preview_num}" if dotted_preview else f"-preview{preview_num}"
    return out


def bump_version(current: str, bump: str) -> str:
    parts, dotted_preview, preview_num = parse_version_parts(current)
    if bump == "major":
        parts[0] += 1
        for i in range(1, len(parts)):
            parts[i] = 0
    elif bump == "minor":
        if len(parts) > 1:
            parts[1] += 1
            for i in range(2, len(parts)):
                parts[i] = 0
        else:
            parts[0] += 1
    else:
        if len(parts) > 2:
            parts[-1] += 1
        elif len(parts) == 2:
            parts[1] += 1
        else:
            parts[0] += 1
    if preview_num is not None:
        preview_num += 1
    return format_version(parts, dotted_preview, preview_num)


def should_skip_header(rel_path: str, skipped_tests: set[str]) -> bool:
    if rel_path.endswith(".Generated.cs"):
        return True
    if MIGRATION_RE.search(rel_path):
        return True
    if MULTIPLE_DB in rel_path.replace("\\", "/"):
        return True
    for marker in TEST_DIR_MARKERS:
        if marker in rel_path.replace("\\", "/"):
            return True
    for test_csproj in skipped_tests:
        if rel_path.startswith(Path(test_csproj).parent.as_posix()):
            return True
    return False


def commit_messages_for_project(root: Path, comparison_range: str, csproj_rel: str) -> list[str]:
    proj_dir = (root / csproj_rel).parent.as_posix() + "/"
    log = run_git(root, ["log", comparison_range, "--format=%s"])
    messages: list[str] = []
    for line in log.splitlines():
        if not line.strip():
            continue
        # include commit if it touched this project directory
        rev_list = run_git(
            root,
            ["log", comparison_range, "--format=%H", f"--grep=.", "--", proj_dir],
        )
        if rev_list.strip():
            # expensive - use simpler: git log range -- proj_dir
            msgs = run_git(root, ["log", comparison_range, "--format=%s", "--", proj_dir]).splitlines()
            return [m for m in msgs if m.strip()]
    return messages


def build_project_plan(
    root: Path,
    csproj_rel: str,
    comparison_range: str,
    commit_summaries: list[dict],
    changed_files: list[str],
) -> ProjectPlan:
    csproj_path = root / csproj_rel
    text = csproj_path.read_text(encoding="utf-8", errors="replace")
    vm = VERSION_TAG_RE.search(text)
    current = vm.group(2).strip() if vm else "0.1.0"

    proj_prefix = csproj_path.parent.as_posix() + "/"
    proj_files = [f for f in changed_files if f.startswith(proj_prefix) or f == csproj_rel]

    messages = run_git(root, ["log", comparison_range, "--format=%s", "--", proj_prefix]).splitlines()
    messages = [m.strip() for m in messages if m.strip()]

    has_window_note = bool(WINDOW_NOTE_RE.search(text))

    feat = any(m.lower().startswith("feat:") for m in messages)
    chore_only = messages and all(m.lower().startswith("chore:") for m in messages)
    bump = "minor" if feat and "Senparc.Xncf" in csproj_rel else "patch"
    if chore_only and not feat:
        bump = "patch"

    details: list[str] = []
    for m in messages:
        if m.lower().startswith("chore: remove redundant code"):
            details.append(
                "Applied standardized copyright headers, XML documentation, and localization metadata across touched sources."
            )
        elif m.lower().startswith("chore: remove unused project"):
            details.append("Removed unused project references and aligned dependent module integration.")
        elif "mcp" in m.lower():
            details.append(m.removeprefix("feat: ").strip().rstrip(".") + ".")
        elif "startchattool" in m.lower():
            details.append(
                "Added StartChatToolTask for admin-driven chat-tool runs with async task monitoring."
            )
        elif m.lower().startswith("feat:"):
            details.append(m.removeprefix("feat: ").strip().rstrip(".") + ".")
        else:
            details.append(m.rstrip(".") + ".")

    if not details:
        if proj_files:
            details.append("Updated functional behavior and package metadata for the current module changes.")
        else:
            details.append("Updated package metadata for related repository changes in the comparison window.")

    # dedupe while preserving order
    seen: set[str] = set()
    unique_details: list[str] = []
    for d in details:
        if d not in seen:
            seen.add(d)
            unique_details.append(d)

    if feat and "AgentsManager" in csproj_rel:
        summary = "Add admin chat-tool task orchestration and MCP-aware chat improvements."
    elif feat and "Connectors" in csproj_rel:
        summary = "Extend MCP connector management and localization coverage."
    elif feat and "MCP" in csproj_rel:
        summary = "Expand MCP endpoint gateway, credentials, and tool invocation support."
    elif feat and "XncfBase" in csproj_rel:
        summary = "Extend shared chat-tool contracts for admin task initiation."
    elif chore_only:
        summary = "Improve source documentation, headers, and module metadata consistency."
    else:
        summary = unique_details[0][:120].rstrip(".")

    if has_window_note:
        new_version = current
        skip_version = True
    else:
        new_version = bump_version(current, bump)
        skip_version = False

    return ProjectPlan(
        path=csproj_rel,
        current_version=current,
        new_version=new_version,
        bump=bump,
        summary=summary,
        details=unique_details[:6],
        skip_version=skip_version,
    )


def append_release_notes(text: str, date_str: str, version: str, summary: str, details: list[str]) -> str:
    if f"[{date_str}] v{version} {summary}" in text:
        return text

    def repl(match: re.Match[str]) -> str:
        open_tag, body, close_tag = match.group(1), match.group(2), match.group(3)
        lines = body.split("\n")
        indent = "        "
        for line in reversed(lines):
            if line.strip():
                indent = re.match(r"^(\s*)", line).group(1)  # type: ignore[union-attr]
                break
        block = [f"{indent}[{date_str}] v{version} {summary}"]
        for i, d in enumerate(details, 1):
            block.append(f"{indent}{i}. {d}")
        addition = "\n" + "\n".join(block)
        if body and not body.endswith("\n"):
            addition = "\n" + addition.lstrip("\n")
        return f"{open_tag}{body}{addition}\n    {close_tag}"

    if NOTES_RE.search(text):
        return NOTES_RE.sub(repl, text, count=1)

    # create minimal notes after Version
    vm = VERSION_TAG_RE.search(text)
    if not vm:
        return text
    insert = (
        f"\n    <PackageReleaseNotes>[{date_str}] v{version} {summary}\n"
        + "\n".join(f"        {i}. {d}" for i, d in enumerate(details, 1))
        + "\n    </PackageReleaseNotes>"
    )
    return text[: vm.end()] + insert + text[vm.end() :]


def update_csproj(path: Path, plan: ProjectPlan, date_str: str, dry_run: bool) -> bool:
    text = path.read_text(encoding="utf-8", errors="replace")
    if plan.skip_version:
        return False

    if plan.new_version != plan.current_version:
        text = VERSION_TAG_RE.sub(
            lambda m: f"{m.group(1)}{plan.new_version}{m.group(3)}",
            text,
            count=1,
        )
    text = append_release_notes(text, date_str, plan.new_version, plan.summary, plan.details)
    if not dry_run:
        path.write_text(text, encoding="utf-8")
    return True


def guess_file_description(content: str, filename: str) -> str:
    m = re.search(r"File description:\s*(.+)", content)
    if m:
        return m.group(1).strip()
    for pat in (
        r"namespace\s+([\w.]+)",
        r"public\s+(?:partial\s+)?class\s+(\w+)",
        r"public\s+interface\s+(\w+)",
    ):
        m2 = re.search(pat, content)
        if m2:
            return f"{m2.group(1)} related implementation"
    return f"{filename} implementation"


def append_or_insert_header(
    path: Path,
    rel: str,
    meta: dict,
    version: str,
    summary: str,
    author: str,
    date_yyyymmdd: str,
    dry_run: bool,
) -> bool:
    content = path.read_text(encoding="utf-8", errors="replace")
    filename = path.name
    short_summary = summary if len(summary) <= 100 else summary[:97] + "..."
    change_line = f"    Change description: v{version} {short_summary}"
    mod_line = f"    Modified by: {author} - {date_yyyymmdd}"

    if change_line in content and mod_line in content:
        return False

    if BLOCK_START in content and HEADER_END in content:
        if mod_line in content:
            return False
        insert_at = content.rfind(HEADER_END)
        before = content[:insert_at].rstrip()
        block = f"\n\n{mod_line}\n{change_line}\n\n"
        new_content = before + block + content[insert_at:]
    else:
        created = meta.get("creation_date_yyyymmdd") or date_yyyymmdd
        desc = guess_file_description(content, filename)
        header = (
            f"{BLOCK_START}\n\n"
            f"    Copyright (C) 2026 Senparc\n\n"
            f"    File name: {filename}\n"
            f"    File description: {desc}\n\n"
            f"    Created by: {author} - {created}\n\n"
            f"{mod_line}\n"
            f"{change_line}\n\n"
            f"{HEADER_END}\n\n"
        )
        new_content = header + content.lstrip("\ufeff")

    if not dry_run:
        path.write_text(new_content, encoding="utf-8")
    return True


def sync_register_version(root: Path, csproj_rel: str, version: str, dry_run: bool) -> None:
    if "Xncf" not in csproj_rel:
        return
    reg_dir = root / Path(csproj_rel).parent
    for reg in reg_dir.glob("Register*.cs"):
        text = reg.read_text(encoding="utf-8", errors="replace")
        new_text, n = re.subn(
            r'(public\s+(?:override\s+)?string\s+Version\s*=>\s*")[^"]+(")',
            rf"\g<1>{version}\g<2>",
            text,
            count=1,
        )
        if n and not dry_run:
            reg.write_text(new_text, encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser(description="Apply Senparc version updater scan results")
    parser.add_argument("--scan", required=True, help="Path to scan JSON")
    parser.add_argument("--author", default="Senparc")
    parser.add_argument("--date", default=None, help="YYYY-MM-DD for release notes")
    parser.add_argument("--header-date", default=None, help="YYYYMMDD for file headers")
    parser.add_argument("--dry-run", action="store_true")
    args = parser.parse_args()

    scan_path = Path(args.scan)
    data = json.loads(scan_path.read_text(encoding="utf-8"))
    root = Path(data["root"])
    comparison_range = data["comparison_range"]
    skipped_tests = set(data.get("skipped_test_csprojs", []))

    today = datetime.now(timezone.utc)
    date_str = args.date or today.strftime("%Y-%m-%d")
    header_date = args.header_date or today.strftime("%Y%m%d")

    changed_files = data.get("changed_files", [])
    plans: dict[str, ProjectPlan] = {}
    for csproj_rel in data.get("changed_csprojs", []):
        if csproj_rel in skipped_tests:
            continue
        plans[csproj_rel] = build_project_plan(
            root, csproj_rel, comparison_range, data.get("commit_summaries", []), changed_files
        )

    # Passive dependents
    for dep in data.get("dependent_csprojs", []):
        if dep in skipped_tests or dep in plans:
            continue
        p = build_project_plan(root, dep, comparison_range, data.get("commit_summaries", []), changed_files)
        p.bump = "patch"
        text = (root / dep).read_text(encoding="utf-8", errors="replace")
        cur = VERSION_TAG_RE.search(text)
        current = cur.group(2).strip() if cur else "0.1.0"
        p.current_version = current
        p.new_version = bump_version(current, "patch")
        p.skip_version = False
        p.summary = "Dependency update for referenced Senparc module version alignment."
        p.details = ["Bumped package version for downstream compatibility with updated module references."]
        plans[dep] = p

    csproj_updates = 0
    for plan in plans.values():
        if update_csproj(root / plan.path, plan, date_str, args.dry_run):
            csproj_updates += 1
            sync_register_version(root, plan.path, plan.new_version, args.dry_run)

    # Map cs file -> project version
    csproj_for_file: dict[str, str] = {}
    mapping = data.get("changed_csproj_to_cs_files", {})
    for csproj, files in mapping.items():
        if csproj in skipped_tests:
            continue
        plan_obj = plans.get(csproj)
        if plan_obj:
            ver = plan_obj.current_version if plan_obj.skip_version else plan_obj.new_version
        else:
            vm = VERSION_TAG_RE.search((root / csproj).read_text(encoding="utf-8", errors="replace"))
            ver = vm.group(2).strip() if vm else "0.0.1"
        for item in files:
            csproj_for_file[item["path"]] = ver

    header_updates = 0
    cs_meta = {item["path"]: item for item in data.get("changed_cs_files", [])}
    for rel, version in csproj_for_file.items():
        if should_skip_header(rel, skipped_tests):
            continue
        path = root / rel
        if not path.exists():
            continue
        plan = None
        for csproj, files in mapping.items():
            if any(f["path"] == rel for f in files):
                plan = plans.get(csproj)
                break
        summary = plan.summary if plan else "Updated module behavior and related support."
        if plan and plan.skip_version:
            version = plan.current_version
        if append_or_insert_header(
            path, rel, cs_meta.get(rel, {}), version, summary, args.author, header_date, args.dry_run
        ):
            header_updates += 1

    print(json.dumps({"csproj_updates": csproj_updates, "header_updates": header_updates, "plans": len(plans)}, indent=2))
    return 0


if __name__ == "__main__":
    sys.exit(main())