#!/usr/bin/env python3
"""Read-only XNCF inventory and architecture boundary checker."""

from __future__ import annotations

import argparse
import json
import re
import sys
import xml.etree.ElementTree as ET
from collections import defaultdict
from dataclasses import asdict, dataclass, field
from pathlib import Path
from typing import Iterable


SKIP_PARTS = {"bin", "obj", "node_modules", ".git", ".vs"}


@dataclass
class Finding:
    severity: str
    code: str
    module: str
    message: str
    file: str | None = None


@dataclass
class Module:
    project: str
    directory: str
    name: str | None = None
    uid: str | None = None
    version: str | None = None
    database_prefix: str | None = None
    capabilities: list[str] = field(default_factory=list)
    project_references: list[str] = field(default_factory=list)
    xncf_dependencies: list[str] = field(default_factory=list)


def is_skipped(path: Path) -> bool:
    return any(part in SKIP_PARTS for part in path.parts)


def relative(path: Path, root: Path) -> str:
    try:
        return str(path.resolve().relative_to(root.resolve()))
    except ValueError:
        return str(path.resolve())


def read_text(path: Path) -> str:
    try:
        return path.read_text(encoding="utf-8-sig")
    except UnicodeDecodeError:
        return path.read_text(encoding="utf-8", errors="replace")


def find_projects(root: Path) -> list[Path]:
    projects: list[Path] = []
    for project in root.rglob("*.csproj"):
        if is_skipped(project.relative_to(root)):
            continue
        lowered = str(project).lower()
        if any(token in lowered for token in (".tests", "tests/", "tests\\")):
            continue
        register_files = list(project.parent.glob("Register*.cs"))
        is_xncf_register = any(re.search(r"\[XncfRegister(?:Attribute)?\]", read_text(item)) for item in register_files)
        if is_xncf_register:
            projects.append(project.resolve())
    return sorted(set(projects))


def parse_project_references(project: Path) -> list[Path]:
    try:
        root = ET.parse(project).getroot()
    except ET.ParseError:
        return []
    references: list[Path] = []
    for element in root.iter():
        if element.tag.split("}")[-1] != "ProjectReference":
            continue
        include = element.attrib.get("Include")
        if include:
            references.append((project.parent / include.replace("\\", "/")).resolve())
    return references


def match_override(text: str, property_name: str) -> str | None:
    pattern = rf"override\s+string\s+{re.escape(property_name)}\s*=>\s*([^;]+);"
    match = re.search(pattern, text)
    if not match:
        return None
    value = match.group(1).strip()
    if len(value) >= 2 and value[0] == value[-1] == '"':
        return value[1:-1]
    return value


def inspect_module(project: Path, root: Path, check_layers: bool) -> tuple[Module, list[Finding]]:
    directory = project.parent
    register_files = sorted(directory.glob("Register*.cs"))
    register_text = "\n".join(read_text(item) for item in register_files)
    module = Module(project=relative(project, root), directory=relative(directory, root))
    findings: list[Finding] = []

    module.name = match_override(register_text, "Name")
    module.uid = match_override(register_text, "Uid")
    module.version = match_override(register_text, "Version")
    prefix = re.search(r"DATABASE_PREFIX\s*=\s*\"([^\"]+)\"", register_text)
    module.database_prefix = prefix.group(1) if prefix else None

    if not register_files:
        findings.append(Finding("error", "missing-register", module.project, "No Register*.cs was found.", module.project))
    if not module.uid:
        findings.append(Finding("review", "missing-literal-uid", module.project, "No literal Register.Uid was found; confirm identity is stable.", module.project))
    elif not re.fullmatch(r"[0-9A-Fa-f]{8}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{12}", module.uid):
        if re.fullmatch(r"[0-9A-Za-z_.]+", module.uid) or "Template_" in module.uid:
            pass
        else:
            findings.append(Finding("review", "nonliteral-uid", module.project, f"Register.Uid is not a literal GUID: {module.uid}", module.project))
    if not module.version:
        findings.append(Finding("review", "missing-literal-version", module.project, "No literal Register.Version was found; confirm release versioning.", module.project))

    source_files = [path for path in directory.rglob("*.cs") if not is_skipped(path.relative_to(directory))]
    capabilities: list[str] = []
    if (directory / "Areas").exists():
        capabilities.append("web")
    if (directory / "Register.Database.cs").exists() or module.database_prefix:
        capabilities.append("database")
    if "FunctionRender" in register_text or any("FunctionRender" in read_text(path) for path in source_files):
        capabilities.append("function")
    if (directory / "OHS" / "Remote").exists():
        capabilities.append("remote-ohs")
    if any(token in read_text(path) for path in source_files for token in ("IIntegrationEvent", "IEventBus", "EventBus")):
        capabilities.append("eventbus")
    if (directory / "wwwroot").exists():
        capabilities.append("static-assets")
    module.capabilities = sorted(set(capabilities))

    references = parse_project_references(project)
    module.project_references = [relative(item, root) for item in references]

    if check_layers:
        layer_rules = {
            "Domain": (".Application", ".OHS", ".Areas"),
            "Application": (".OHS", ".Areas"),
        }
        for layer, forbidden in layer_rules.items():
            layer_root = directory / layer
            if not layer_root.exists():
                continue
            for source in layer_root.rglob("*.cs"):
                if is_skipped(source.relative_to(directory)) or source.name.endswith(".Designer.cs") or "Migrations" in source.parts:
                    continue
                text = read_text(source)
                hits = sorted(token for token in forbidden if re.search(rf"^\s*using\s+[^;]*{re.escape(token)}(?:\.|;)", text, re.MULTILINE))
                if hits:
                    findings.append(Finding(
                        "review",
                        "layer-direction",
                        module.project,
                        f"{layer} imports higher/adaptor namespace(s): {', '.join(hits)}",
                        relative(source, root),
                    ))

    return module, findings


def find_cycles(edges: dict[str, list[str]]) -> list[list[str]]:
    cycles: set[tuple[str, ...]] = set()
    visiting: list[str] = []
    active: set[str] = set()
    visited: set[str] = set()

    def canonical(cycle: list[str]) -> tuple[str, ...]:
        body = cycle[:-1]
        rotations = [tuple(body[index:] + body[:index]) for index in range(len(body))]
        smallest = min(rotations)
        return smallest + (smallest[0],)

    def walk(node: str) -> None:
        if node in active:
            index = visiting.index(node)
            cycles.add(canonical(visiting[index:] + [node]))
            return
        if node in visited:
            return
        active.add(node)
        visiting.append(node)
        for dependency in edges.get(node, []):
            walk(dependency)
        visiting.pop()
        active.remove(node)
        visited.add(node)

    for node in edges:
        walk(node)
    return [list(item) for item in sorted(cycles)]


def module_kind(module: Module) -> str:
    path = module.project.replace("\\", "/")
    if path.startswith("src/Basic/") or "/src/Basic/" in path:
        return "framework"
    if path.startswith("src/Extensions/System/") or "/src/Extensions/System/" in path:
        return "system"
    if "templates/template1/" in path or "Template_OrgName" in path:
        return "template"
    return "product"


def inspect(root: Path, check_layers: bool = False) -> tuple[list[Module], list[Finding]]:
    projects = find_projects(root)
    modules: list[Module] = []
    findings: list[Finding] = []
    project_to_module: dict[Path, Module] = {}

    for project in projects:
        module, module_findings = inspect_module(project, root, check_layers)
        modules.append(module)
        project_to_module[project] = module
        findings.extend(module_findings)

    for project, module in project_to_module.items():
        dependencies: list[str] = []
        for reference in parse_project_references(project):
            dependency = project_to_module.get(reference)
            if not dependency:
                continue
            if (
                module_kind(dependency) == "product"
                and dependency.project != module.project
                and dependency.directory != module.directory
            ):
                dependencies.append(dependency.project)
                if ".Abstractions" not in dependency.project:
                    findings.append(Finding(
                        "review",
                        "direct-xncf-reference",
                        module.project,
                        f"Directly references XNCF implementation {dependency.project}; confirm the shared lifecycle or extract a contract.",
                        module.project,
                    ))
        module.xncf_dependencies = sorted(set(dependencies))

    by_uid: dict[str, list[Module]] = defaultdict(list)
    by_prefix: dict[str, list[Module]] = defaultdict(list)
    for module in modules:
        if module.uid and re.fullmatch(r"[0-9A-Fa-f-]{36}", module.uid):
            by_uid[module.uid.upper()].append(module)
        if module.database_prefix and "Template_" not in module.database_prefix:
            by_prefix[module.database_prefix.lower()].append(module)
    for uid, owners in by_uid.items():
        distinct_owners = {item.directory for item in owners}
        if len(distinct_owners) > 1:
            names = ", ".join(item.project for item in owners)
            findings.append(Finding("error", "duplicate-uid", names, f"UID {uid} is used by multiple modules: {names}"))
    for prefix, owners in by_prefix.items():
        distinct_owners = {item.directory for item in owners}
        if len(distinct_owners) > 1:
            names = ", ".join(item.project for item in owners)
            findings.append(Finding("error", "duplicate-database-prefix", names, f"Database prefix {prefix} is used by multiple modules: {names}"))

    edges = {module.project: module.xncf_dependencies for module in modules}
    for cycle in find_cycles(edges):
        findings.append(Finding("error", "xncf-dependency-cycle", cycle[0], " -> ".join(cycle)))

    findings.sort(key=lambda item: (item.severity != "error", item.code, item.module, item.file or ""))
    return modules, findings


def print_markdown(root: Path, modules: Iterable[Module], findings: Iterable[Finding]) -> None:
    modules = list(modules)
    findings = list(findings)
    print(f"# XNCF inventory: {root.resolve()}")
    print()
    print("| Project | Version | UID | DB prefix | Capabilities | XNCF dependencies |")
    print("| --- | --- | --- | --- | --- | --- |")
    for module in modules:
        print("| {project} | {version} | {uid} | {prefix} | {capabilities} | {dependencies} |".format(
            project=module.project,
            version=module.version or "-",
            uid=module.uid or "-",
            prefix=module.database_prefix or "-",
            capabilities=", ".join(module.capabilities) or "-",
            dependencies=", ".join(module.xncf_dependencies) or "-",
        ))
    print()
    print(f"## Findings ({len(findings)})")
    print()
    if not findings:
        print("No findings.")
        return
    for item in findings:
        location = f" ({item.file})" if item.file else ""
        print(f"- **{item.severity.upper()} `{item.code}`** [{item.module}]{location}: {item.message}")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("root", nargs="?", default=".", help="Repository root to inspect")
    parser.add_argument("--format", choices=("markdown", "json"), default="markdown")
    parser.add_argument("--module", action="append", default=[], help="Only print modules/findings whose project path contains this text")
    parser.add_argument("--layers", action="store_true", help="Also report Domain/Application namespace direction findings")
    parser.add_argument("--strict", action="store_true", help="Exit non-zero when any finding exists")
    args = parser.parse_args()

    root = Path(args.root).expanduser().resolve()
    if not root.is_dir():
        parser.error(f"Not a directory: {root}")

    modules, findings = inspect(root, check_layers=args.layers)
    if args.module:
        needles = [item.lower() for item in args.module]
        modules = [item for item in modules if any(needle in item.project.lower() for needle in needles)]
        findings = [item for item in findings if any(needle in item.module.lower() for needle in needles)]
    if args.format == "json":
        print(json.dumps({
            "root": str(root),
            "modules": [asdict(item) for item in modules],
            "findings": [asdict(item) for item in findings],
        }, ensure_ascii=False, indent=2))
    else:
        print_markdown(root, modules, findings)

    if args.strict and findings:
        return 1
    if any(item.severity == "error" for item in findings):
        return 2
    return 0


if __name__ == "__main__":
    sys.exit(main())
