#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import re
import subprocess
import sys
import xml.etree.ElementTree as ET
from collections import defaultdict
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path


IGNORED_DIRS = {".git", ".idea", ".vs", ".vscode", "bin", "obj"}
NET_FILENAME_RE = re.compile(r"\.net(?P<major>\d+)\.csproj$", re.IGNORECASE)
NET_TFM_RE = re.compile(r"^net(?P<major>\d+)(?:\.\d+)?(?:[-].*)?$", re.IGNORECASE)
MSBUILD_THIS_FILE_DIRECTORY_RE = re.compile(r"\$\(MSBuildThisFileDirectory\)", re.IGNORECASE)
MSBUILD_PROJECT_DIRECTORY_RE = re.compile(r"\$\(MSBuildProjectDirectory\)", re.IGNORECASE)
TIMESTAMP_TOKEN_RE = re.compile(
    r"\b\d{4}[-/]\d{1,2}[-/]\d{1,2}(?:[T ]\d{1,2}:\d{2}(?::\d{2})?(?:\.\d+)?(?:Z|[+-]\d{2}:?\d{2})?)?\b"
    r"|\b\d{1,2}:\d{2}(?::\d{2})?\b"
)
COMPACT_TIMESTAMP_RE = re.compile(r"\b20\d{6}(?:\d{6})?\b")
TIMESTAMP_HINT_RE = re.compile(
    r"(generated|generation|timestamp|datetime|date|time|created|updated|createdat|updatedat|"
    r"创建时间|更新时间|生成时间)",
    re.IGNORECASE,
)


@dataclass(frozen=True)
class CsprojInfo:
    path: Path
    tfm_majors: list[int]
    project_references: list[Path]
    imported_props: list[Path]
    current_version: str | None


def _stderr(message: str) -> None:
    print(message, file=sys.stderr)


def run_git(root: Path, args: list[str]) -> tuple[int, str]:
    proc = subprocess.run(
        ["git", "-C", str(root), *args],
        capture_output=True,
        text=True,
        check=False,
    )
    # Preserve porcelain status' leading XY column; only remove record-ending newlines.
    return proc.returncode, proc.stdout.rstrip("\r\n")


def local_name(tag: str) -> str:
    if "}" in tag:
        return tag.rsplit("}", 1)[-1]
    return tag


def extract_tfm_majors(raw_frameworks: list[str]) -> list[int]:
    majors: set[int] = set()
    for raw in raw_frameworks:
        for tfm in raw.split(";"):
            tfm = tfm.strip()
            if not tfm:
                continue
            match = NET_TFM_RE.match(tfm)
            if not match:
                continue
            majors.add(int(match.group("major")))
    return sorted(majors)


def resolve_props_import(importer_path: Path, raw_project: str) -> Path | None:
    value = raw_project.strip()
    if not value:
        return None

    importer_dir = importer_path.parent.resolve()
    value = MSBUILD_THIS_FILE_DIRECTORY_RE.sub(f"{importer_dir.as_posix()}/", value)
    value = MSBUILD_PROJECT_DIRECTORY_RE.sub(importer_dir.as_posix(), value)
    value = value.replace("\\", "/")

    # Other MSBuild properties and wildcard imports cannot be resolved statically.
    if "$(" in value or "@(" in value or "%(" in value or any(token in value for token in ("*", "?")):
        return None

    candidate = Path(value)
    if not candidate.is_absolute():
        candidate = importer_dir / candidate
    candidate = candidate.resolve()
    if candidate.suffix.lower() != ".props":
        return None
    return candidate


def parse_imported_props(importer_path: Path, xml_root: ET.Element | None = None) -> list[Path]:
    if xml_root is None:
        try:
            xml_root = ET.parse(importer_path).getroot()
        except (ET.ParseError, OSError):
            return []

    imports: set[Path] = set()
    for elem in xml_root.iter():
        if local_name(elem.tag) != "Import":
            continue
        resolved = resolve_props_import(importer_path, elem.attrib.get("Project", ""))
        if resolved is not None:
            imports.add(resolved)
    return sorted(imports)


def parse_csproj(csproj_path: Path) -> CsprojInfo:
    tfm_values: list[str] = []
    references: list[Path] = []
    version: str | None = None

    try:
        tree = ET.parse(csproj_path)
    except ET.ParseError:
        return CsprojInfo(
            path=csproj_path,
            tfm_majors=[],
            project_references=[],
            imported_props=[],
            current_version=None,
        )

    root = tree.getroot()
    for elem in root.iter():
        name = local_name(elem.tag)
        text = (elem.text or "").strip()
        if name in {"TargetFramework", "TargetFrameworks"} and text:
            tfm_values.append(text)
        elif name == "ProjectReference":
            include = elem.attrib.get("Include", "").strip()
            if include:
                include = include.replace("\\", "/")
                ref = (csproj_path.parent / include).resolve()
                references.append(ref)
        elif name in {"Version", "PackageVersion", "AssemblyVersion", "FileVersion"} and text and version is None:
            version = text

    return CsprojInfo(
        path=csproj_path.resolve(),
        tfm_majors=extract_tfm_majors(tfm_values),
        project_references=references,
        imported_props=parse_imported_props(csproj_path, root),
        current_version=version,
    )


def discover_csproj_files(root: Path) -> list[Path]:
    result: list[Path] = []
    for path in root.rglob("*.csproj"):
        if any(part in IGNORED_DIRS for part in path.parts):
            continue
        if not path.is_file():
            continue
        result.append(path.resolve())
    return sorted(result)


def discover_props_files(root: Path) -> list[Path]:
    result: list[Path] = []
    for path in root.rglob("*.props"):
        if any(part in IGNORED_DIRS for part in path.parts):
            continue
        if path.is_file():
            result.append(path.resolve())
    return sorted(result)


def filename_net_major(path: Path) -> int:
    match = NET_FILENAME_RE.search(path.name)
    return int(match.group("major")) if match else -1


def tfm_major(info: CsprojInfo) -> int:
    if not info.tfm_majors:
        return -1
    return max(info.tfm_majors)


def select_csproj_for_directory(
    project_dir: Path,
    candidates: list[Path],
    info_by_path: dict[Path, CsprojInfo],
    warnings: list[str],
) -> Path:
    name_candidates = [p for p in candidates if filename_net_major(p) >= 10]
    if name_candidates:
        return sorted(name_candidates, key=lambda p: (-filename_net_major(p), p.name))[0]

    tfm_candidates = [p for p in candidates if tfm_major(info_by_path[p]) >= 10]
    if tfm_candidates:
        return sorted(tfm_candidates, key=lambda p: (-tfm_major(info_by_path[p]), p.name))[0]

    if len(candidates) == 1:
        return candidates[0]

    warnings.append(
        (
            f"Directory '{project_dir}' has multiple .csproj files but no net10+ candidate. "
            "Fallback to highest TargetFramework major version."
        )
    )
    return sorted(candidates, key=lambda p: (-tfm_major(info_by_path[p]), p.name))[0]


def build_selected_csproj_map(csproj_paths: list[Path], info_by_path: dict[Path, CsprojInfo]) -> tuple[dict[Path, Path], list[str]]:
    by_dir: dict[Path, list[Path]] = defaultdict(list)
    for path in csproj_paths:
        by_dir[path.parent].append(path)

    warnings: list[str] = []
    selected: dict[Path, Path] = {}
    for project_dir, candidates in by_dir.items():
        selected[project_dir] = select_csproj_for_directory(project_dir, candidates, info_by_path, warnings)
    return selected, warnings


def normalize_user_path(root: Path, user_path: str) -> Path:
    candidate = Path(user_path).expanduser()
    if not candidate.is_absolute():
        candidate = (root / candidate).resolve()
    else:
        candidate = candidate.resolve()
    return candidate


def resolve_primary_csproj(project_path: Path, selected_by_dir: dict[Path, Path]) -> Path:
    if project_path.is_dir():
        if project_path in selected_by_dir:
            return selected_by_dir[project_path]
        raise FileNotFoundError(f"No .csproj files discovered under directory: {project_path}")

    if project_path.suffix.lower() != ".csproj":
        raise ValueError(f"Path is not a .csproj file: {project_path}")

    project_dir = project_path.parent
    if project_dir in selected_by_dir:
        return selected_by_dir[project_dir]
    if project_path.exists():
        return project_path
    raise FileNotFoundError(f"Requested .csproj does not exist: {project_path}")


def to_relative_path(root: Path, file_path: Path) -> str:
    try:
        return file_path.resolve().relative_to(root.resolve()).as_posix()
    except ValueError:
        return file_path.resolve().as_posix()


def resolve_baseline_branch_ref(root: Path) -> str | None:
    candidates = [
        "refs/remotes/origin/master",
        "refs/remotes/origin/main",
        "refs/heads/master",
        "refs/heads/main",
        "origin/master",
        "origin/main",
        "master",
        "main",
    ]
    for ref in candidates:
        code, _ = run_git(root, ["rev-parse", "--verify", "--quiet", ref])
        if code == 0:
            return ref
    return None


def resolve_master_main_comparison_base(root: Path) -> tuple[str, str]:
    baseline_ref = resolve_baseline_branch_ref(root)
    if not baseline_ref:
        raise RuntimeError(
            "Cannot resolve comparison baseline branch from master/main. "
            "Expected one of: origin/master, origin/main, master, main."
        )

    code, output = run_git(root, ["merge-base", "HEAD", baseline_ref])
    merge_base = output.splitlines()[0].strip() if output else ""
    if code != 0 or not merge_base:
        raise RuntimeError(f"Cannot compute merge-base between HEAD and {baseline_ref}.")

    return baseline_ref, merge_base


def get_commit_epoch_seconds(root: Path, commit: str) -> int | None:
    code, output = run_git(root, ["show", "-s", "--format=%ct", commit])
    if code == 0 and output.isdigit():
        return int(output)
    return None


def get_last_csproj_change(root: Path, csproj: Path) -> tuple[str | None, int]:
    rel = to_relative_path(root, csproj)
    code, output = run_git(root, ["log", "-1", "--format=%H|%ct", "--", rel])
    if code == 0 and output and "|" in output:
        commit_hash, epoch_str = output.split("|", 1)
        if commit_hash and epoch_str.isdigit():
            return commit_hash, int(epoch_str)
    return None, int(csproj.stat().st_mtime)


def parse_git_status_paths(status_text: str) -> set[str]:
    paths: set[str] = set()
    for line in status_text.splitlines():
        if len(line) < 4:
            continue
        raw = line[3:].strip()
        if " -> " in raw:
            raw = raw.split(" -> ", 1)[-1].strip()
        if raw:
            paths.add(raw.replace("\\", "/"))
    return paths


def list_changed_paths(root: Path, base_commit: str | None, path_scope: str | None = None) -> list[str]:
    changed: set[str] = set()

    if base_commit:
        diff_args = ["diff", "--name-only", f"{base_commit}..HEAD"]
        if path_scope:
            diff_args.extend(["--", path_scope])
        code, output = run_git(root, diff_args)
        if code == 0 and output:
            changed.update(line.strip().replace("\\", "/") for line in output.splitlines() if line.strip())

    status_args = ["status", "--porcelain"]
    if path_scope:
        status_args.extend(["--", path_scope])
    code, status_output = run_git(root, status_args)
    if code == 0 and status_output:
        changed.update(parse_git_status_paths(status_output))

    return sorted(changed)


def is_generated_cs(path: str) -> bool:
    return path.lower().endswith(".generated.cs")


def read_git_file_text(root: Path, revision: str, rel_path: str) -> str | None:
    proc = subprocess.run(
        ["git", "-C", str(root), "show", f"{revision}:{rel_path}"],
        capture_output=True,
        text=True,
        check=False,
    )
    if proc.returncode != 0:
        return None
    return proc.stdout


def read_worktree_file_text(root: Path, rel_path: str) -> str | None:
    abs_path = (root / rel_path).resolve()
    if not abs_path.exists() or not abs_path.is_file():
        return None
    return abs_path.read_text(encoding="utf-8", errors="replace")


def normalize_generated_content(text: str) -> str:
    normalized_lines: list[str] = []
    for raw_line in text.splitlines():
        line = TIMESTAMP_TOKEN_RE.sub("<TS>", raw_line)
        if TIMESTAMP_HINT_RE.search(line):
            line = COMPACT_TIMESTAMP_RE.sub("<TS>", line)
        normalized_lines.append(line.rstrip())
    return "\n".join(normalized_lines).strip()


def is_timestamp_only_generated_change(root: Path, rel_path: str, base_commit: str | None) -> bool:
    if not is_generated_cs(rel_path):
        return False

    baseline_revision = base_commit if base_commit else "HEAD"
    baseline_text = read_git_file_text(root, baseline_revision, rel_path)
    if baseline_text is None:
        # New file or no baseline: keep as changed.
        return False

    current_text = read_worktree_file_text(root, rel_path)
    if current_text is None:
        # Deletion/rename target not present: keep as changed.
        return False

    return normalize_generated_content(baseline_text) == normalize_generated_content(current_text)


def filter_ignorable_generated_changes(
    root: Path,
    changed_files: list[str],
    base_commit: str | None,
) -> tuple[list[str], list[str]]:
    kept: list[str] = []
    ignored: list[str] = []
    for rel_path in changed_files:
        if is_timestamp_only_generated_change(root, rel_path, base_commit):
            ignored.append(rel_path)
            continue
        kept.append(rel_path)
    return kept, ignored


def list_commit_summaries(root: Path, base_commit: str | None, path_scope: str | None = None) -> list[dict[str, str]]:
    if not base_commit:
        return []

    log_args = ["log", "--reverse", "--date=short", "--format=%h|%ad|%s", f"{base_commit}..HEAD"]
    if path_scope:
        log_args.extend(["--", path_scope])
    code, output = run_git(
        root,
        log_args,
    )
    if code != 0 or not output:
        return []

    summaries: list[dict[str, str]] = []
    for line in output.splitlines():
        parts = line.split("|", 2)
        if len(parts) != 3:
            continue
        summaries.append({"commit": parts[0], "date": parts[1], "message": parts[2]})
    return summaries


def path_is_under(child_rel: str, parent_rel: str) -> bool:
    child = child_rel.replace("\\", "/").strip("/")
    parent = parent_rel.replace("\\", "/").strip("/")
    if not parent:
        return True
    return child == parent or child.startswith(parent + "/")


def resolve_selected_csproj_for_file(file_path: Path, selected_by_dir: dict[Path, Path], root: Path) -> Path | None:
    current = file_path.resolve().parent
    root_resolved = root.resolve()
    while True:
        selected = selected_by_dir.get(current)
        if selected is not None:
            return selected
        if current == root_resolved or current.parent == current:
            return None
        current = current.parent


def get_file_creation_date_yyyymmdd(root: Path, file_path: Path) -> str:
    rel = to_relative_path(root, file_path)
    code, output = run_git(root, ["log", "--follow", "--diff-filter=A", "--format=%cd", "--date=format:%Y%m%d", "--", rel])
    if code == 0 and output:
        lines = [line.strip() for line in output.splitlines() if line.strip()]
        if lines:
            return lines[-1]
    return datetime.fromtimestamp(file_path.stat().st_mtime, tz=timezone.utc).strftime("%Y%m%d")


def build_reverse_dependency_map(selected_by_dir: dict[Path, Path], info_by_path: dict[Path, CsprojInfo]) -> dict[Path, set[Path]]:
    selected_set = set(selected_by_dir.values())
    reverse_map: dict[Path, set[Path]] = defaultdict(set)

    for src in selected_set:
        info = info_by_path[src]
        for raw_ref in info.project_references:
            ref_path = raw_ref.resolve()
            target = selected_by_dir.get(ref_path.parent)
            if target is None and ref_path in selected_set:
                target = ref_path
            if target is None:
                continue
            reverse_map[target].add(src)

    return reverse_map


def build_reverse_props_import_map(
    csproj_paths: list[Path],
    props_paths: list[Path],
    selected_by_dir: dict[Path, Path],
    info_by_path: dict[Path, CsprojInfo],
) -> dict[Path, set[Path]]:
    reverse_map: dict[Path, set[Path]] = defaultdict(set)

    for csproj in csproj_paths:
        selected = selected_by_dir.get(csproj.parent, csproj).resolve()
        for imported_props in info_by_path[csproj].imported_props:
            reverse_map[imported_props.resolve()].add(selected)

    for props in props_paths:
        for imported_props in parse_imported_props(props):
            reverse_map[imported_props.resolve()].add(props.resolve())

    return reverse_map


def resolve_changed_props_importers(
    changed_props: list[Path],
    reverse_import_map: dict[Path, set[Path]],
) -> dict[Path, set[Path]]:
    result: dict[Path, set[Path]] = {}

    for changed_props_path in changed_props:
        affected_projects: set[Path] = set()
        visited_props: set[Path] = {changed_props_path.resolve()}
        current_layer: set[Path] = {changed_props_path.resolve()}

        while current_layer:
            next_layer: set[Path] = set()
            for imported_file in current_layer:
                for importer in reverse_import_map.get(imported_file, set()):
                    if importer.suffix.lower() == ".csproj":
                        affected_projects.add(importer.resolve())
                    elif importer.suffix.lower() == ".props" and importer not in visited_props:
                        visited_props.add(importer)
                        next_layer.add(importer)
            current_layer = next_layer

        result[changed_props_path.resolve()] = affected_projects

    return result


def compute_dependency_layers(
    roots: set[Path],
    reverse_map: dict[Path, set[Path]],
) -> tuple[list[list[Path]], list[Path]]:
    layers: list[list[Path]] = []
    visited: set[Path] = {root.resolve() for root in roots}
    current_layer: set[Path] = set(visited)

    while True:
        next_layer: set[Path] = set()
        for project in current_layer:
            for dependent in reverse_map.get(project, set()):
                if dependent in visited:
                    continue
                visited.add(dependent)
                next_layer.add(dependent)
        if not next_layer:
            break
        sorted_layer = sorted(next_layer)
        layers.append(sorted_layer)
        current_layer = next_layer

    flattened = [item for layer in layers for item in layer]
    return layers, flattened


def main() -> int:
    parser = argparse.ArgumentParser(
        description=(
            "Scan Senparc project metadata for version updates: choose target csproj, "
            "list changed files since merge-base against master/main, and compute recursive dependent projects."
        )
    )
    parser.add_argument("--root", required=True, help="Repository root path")
    parser.add_argument("--project", required=True, help="Target project directory or .csproj path")
    parser.add_argument("--indent", type=int, default=2, help="JSON indentation spaces")
    args = parser.parse_args()

    root = normalize_user_path(Path.cwd(), args.root)
    if not root.exists() or not root.is_dir():
        _stderr(f"Invalid --root directory: {root}")
        return 1

    csproj_paths = discover_csproj_files(root)
    if not csproj_paths:
        _stderr(f"No .csproj files found under: {root}")
        return 1
    props_paths = discover_props_files(root)

    info_by_path = {path: parse_csproj(path) for path in csproj_paths}
    selected_by_dir, warnings = build_selected_csproj_map(csproj_paths, info_by_path)
    reverse_props_import_map = build_reverse_props_import_map(
        csproj_paths,
        props_paths,
        selected_by_dir,
        info_by_path,
    )

    requested = normalize_user_path(root, args.project)
    try:
        primary = resolve_primary_csproj(requested, selected_by_dir).resolve()
    except (FileNotFoundError, ValueError) as ex:
        _stderr(str(ex))
        return 1

    try:
        comparison_branch_ref, comparison_base_commit = resolve_master_main_comparison_base(root)
    except RuntimeError as ex:
        _stderr(str(ex))
        return 1

    comparison_base_epoch = get_commit_epoch_seconds(root, comparison_base_commit)
    if comparison_base_epoch is None:
        _stderr(f"Cannot resolve commit epoch for comparison base commit: {comparison_base_commit}")
        return 1

    primary_info = info_by_path.get(primary) or parse_csproj(primary)
    primary_last_commit, primary_last_epoch = get_last_csproj_change(root, primary)
    changed_files = list_changed_paths(root, comparison_base_commit)
    changed_files, ignored_generated_cs_files = filter_ignorable_generated_changes(root, changed_files, comparison_base_commit)
    commit_summaries = list_commit_summaries(root, comparison_base_commit)

    changed_cs_files: list[dict[str, str]] = []
    changed_csproj_targets: set[Path] = set()
    changed_csproj_to_cs_files: dict[Path, list[dict[str, str]]] = defaultdict(list)
    changed_props_files = [rel for rel in changed_files if rel.lower().endswith(".props")]
    changed_props_paths = [(root / rel).resolve() for rel in changed_props_files]
    changed_props_importers = resolve_changed_props_importers(changed_props_paths, reverse_props_import_map)
    changed_csproj_to_props_files: dict[Path, list[str]] = defaultdict(list)

    for changed_props_path, importers in changed_props_importers.items():
        rel_props = to_relative_path(root, changed_props_path)
        for importer in importers:
            importer = importer.resolve()
            changed_csproj_targets.add(importer)
            changed_csproj_to_props_files[importer].append(rel_props)

    for rel in changed_files:
        if not rel.lower().endswith(".cs"):
            continue
        if is_generated_cs(rel):
            continue
        abs_file = (root / rel).resolve()
        if not abs_file.exists():
            continue
        cs_file_item = {
            "path": rel,
            "creation_date_yyyymmdd": get_file_creation_date_yyyymmdd(root, abs_file),
        }
        mapped_csproj = resolve_selected_csproj_for_file(abs_file, selected_by_dir, root)
        if mapped_csproj is not None:
            mapped_csproj = mapped_csproj.resolve()
            changed_csproj_targets.add(mapped_csproj)
            changed_csproj_to_cs_files[mapped_csproj].append(cs_file_item)
        changed_cs_files.append(cs_file_item)

    primary_project_rel = to_relative_path(root, primary.parent)
    changed_files_in_primary_project = [rel for rel in changed_files if path_is_under(rel, primary_project_rel)]
    changed_cs_files_in_primary_project = [item for item in changed_cs_files if path_is_under(item["path"], primary_project_rel)]

    reverse_map = build_reverse_dependency_map(selected_by_dir, info_by_path)
    dependency_roots = set(changed_csproj_targets) if changed_csproj_targets else {primary}
    dependency_layers, dependents = compute_dependency_layers(dependency_roots, reverse_map)

    payload = {
        "root": root.resolve().as_posix(),
        "requested_project": requested.as_posix(),
        "primary_csproj": to_relative_path(root, primary),
        "primary_current_version": primary_info.current_version,
        "comparison_base": {
            "branch_ref": comparison_branch_ref,
            "commit": comparison_base_commit,
            "epoch_seconds": comparison_base_epoch,
            "utc": datetime.fromtimestamp(comparison_base_epoch, tz=timezone.utc).strftime("%Y-%m-%d %H:%M:%S UTC"),
        },
        "comparison_range": f"{comparison_base_commit}..HEAD",
        "primary_last_change": {
            "commit": primary_last_commit,
            "epoch_seconds": primary_last_epoch,
            "utc": datetime.fromtimestamp(primary_last_epoch, tz=timezone.utc).strftime("%Y-%m-%d %H:%M:%S UTC"),
        },
        "changed_files": changed_files,
        "changed_cs_files": changed_cs_files,
        "changed_props_files": changed_props_files,
        "changed_csprojs": [to_relative_path(root, p) for p in sorted(changed_csproj_targets)],
        "changed_csproj_to_cs_files": {
            to_relative_path(root, project): sorted(items, key=lambda item: item["path"])
            for project, items in sorted(changed_csproj_to_cs_files.items(), key=lambda kv: to_relative_path(root, kv[0]))
        },
        "changed_props_importers": {
            to_relative_path(root, props): [to_relative_path(root, project) for project in sorted(projects)]
            for props, projects in sorted(changed_props_importers.items(), key=lambda kv: to_relative_path(root, kv[0]))
        },
        "changed_csproj_to_props_files": {
            to_relative_path(root, project): sorted(set(items))
            for project, items in sorted(changed_csproj_to_props_files.items(), key=lambda kv: to_relative_path(root, kv[0]))
        },
        "changed_files_in_primary_project": changed_files_in_primary_project,
        "changed_cs_files_in_primary_project": changed_cs_files_in_primary_project,
        "commit_summaries": commit_summaries,
        "ignored_generated_cs_files": ignored_generated_cs_files,
        "dependency_roots": [to_relative_path(root, p) for p in sorted(dependency_roots)],
        "dependent_layers": [[to_relative_path(root, p) for p in layer] for layer in dependency_layers],
        "dependent_csprojs": [to_relative_path(root, p) for p in dependents],
        "warnings": warnings,
    }

    print(json.dumps(payload, ensure_ascii=False, indent=args.indent))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
