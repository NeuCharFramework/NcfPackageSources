#!/usr/bin/env python3
"""Prepare an XNCF identity and a `dotnet new XNCF` command for a published NCF site.

The host is the directory that contains Senparc.Web. Templates and Senparc
packages come from NuGet. This script does not read or require a package
source tree.
"""

from __future__ import annotations

import argparse
import json
import re
import subprocess
import sys
import urllib.request
import uuid
from pathlib import Path


SKIP_PARTS = {"bin", "obj", "node_modules", ".git", ".vs"}
IDENTIFIER = re.compile(r"^[A-Za-z][A-Za-z0-9]*$")
VERSION = re.compile(r"^\d+\.\d+(\.\d+)?([-+][0-9A-Za-z.-]+)?$")
UID_PATTERN = re.compile(r'override\s+string\s+Uid\s*=>\s*"([^"]+)"')
PREFIX_PATTERN = re.compile(r'DATABASE_PREFIX\s*=\s*"([^"]+)"')
TARGET_FRAMEWORK_PATTERN = re.compile(r"<TargetFramework>\s*([^<]+?)\s*</TargetFramework>")
ZERO_GUID = "00000000-0000-0000-0000-000000000000"
TEMPLATE_PACKAGE = "Senparc.Xncf.XncfBuilder.Template"
CORE_PACKAGES = (
    "Senparc.Ncf.XncfBase",
    "Senparc.Ncf.AreaBase",
    "Senparc.Ncf.DatabasePlant",
    "Senparc.Xncf.AreasBase",
)


def is_skipped(path: Path) -> bool:
    return any(part in SKIP_PARTS for part in path.parts)


def read_text(path: Path) -> str:
    try:
        return path.read_text(encoding="utf-8-sig")
    except UnicodeDecodeError:
        return path.read_text(encoding="utf-8", errors="replace")


def strip_xml_comments(text: str) -> str:
    return re.sub(r"<!--.*?-->", "", text, flags=re.DOTALL)


def quote(value: str) -> str:
    if re.fullmatch(r"[A-Za-z0-9_./:+-]+", value):
        return value
    return "'" + value.replace("'", "'\"'\"'") + "'"


def package_references(text: str) -> dict[str, str]:
    text = strip_xml_comments(text)
    found: dict[str, str] = {}
    for match in re.finditer(r"<PackageReference\b([^>]*)/?>", text, re.IGNORECASE):
        attrs = match.group(1)
        include = re.search(r'Include="([^"]+)"', attrs)
        version = re.search(r'Version="([^"]+)"', attrs)
        if include and version and include.group(1).startswith("Senparc."):
            found[include.group(1)] = version.group(1)
    for match in re.finditer(
        r'<PackageReference\b[^>]*Include="([^"]+)"[^>]*>\s*<Version>\s*([^<]+?)\s*</Version>',
        text,
        re.IGNORECASE,
    ):
        if match.group(1).startswith("Senparc."):
            found.setdefault(match.group(1), match.group(2).strip())
    return found


def versions_from_assets(path: Path) -> dict[str, str]:
    if not path.is_file():
        return {}
    try:
        data = json.loads(read_text(path))
    except json.JSONDecodeError:
        return {}
    found: dict[str, str] = {}
    targets = data.get("targets") or {}
    for libraries in targets.values():
        if not isinstance(libraries, dict):
            continue
        for key in libraries:
            if "/" not in key:
                continue
            name, version = key.split("/", 1)
            if name.startswith("Senparc.Ncf.") or name.startswith("Senparc.Xncf."):
                found[name] = version
    return found


def nuget_latest(package_id: str) -> str | None:
    url = f"https://api.nuget.org/v3-flatcontainer/{package_id.lower()}/index.json"
    try:
        with urllib.request.urlopen(url, timeout=20) as response:
            payload = json.load(response)
    except Exception as ex:  # noqa: BLE001 - report the lookup failure in the plan
        print(f"NuGet lookup failed for {package_id}: {ex}", file=sys.stderr)
        return None
    versions = payload.get("versions") or []
    return versions[-1] if versions else None


def find_web_projects(root: Path) -> list[Path]:
    found: list[Path] = []
    direct = [root / "Senparc.Web.csproj", root / "Senparc.Web" / "Senparc.Web.csproj"]
    found.extend(path for path in direct if path.is_file())
    for path in root.rglob("Senparc.Web.csproj"):
        try:
            relative = path.relative_to(root)
        except ValueError:
            continue
        if is_skipped(relative) or len(relative.parts) > 6:
            continue
        found.append(path)
    unique = list(dict.fromkeys(path.resolve() for path in found))

    def rank(path: Path) -> tuple[int, int]:
        references = package_references(read_text(path))
        return (len(references), -len(path.parts))

    return sorted(unique, key=rank, reverse=True)


def find_solution(web_project: Path) -> Path | None:
    solution_dir = web_project.parent.parent
    candidates = sorted(solution_dir.glob("*.sln"))
    for candidate in candidates:
        if "Senparc.Web" in read_text(candidate):
            return candidate
    return candidates[0] if candidates else None


def scan_identities(root: Path) -> tuple[dict[str, str], dict[str, str]]:
    uids: dict[str, str] = {}
    prefixes: dict[str, str] = {}
    for register in root.rglob("Register*.cs"):
        try:
            relative_path = register.relative_to(root)
        except ValueError:
            continue
        if is_skipped(relative_path):
            continue
        text = read_text(register)
        relative = str(relative_path)
        for uid in UID_PATTERN.findall(text):
            if "Template_" in uid or uid in {"WrongGuid", ZERO_GUID}:
                continue
            uids.setdefault(uid.upper(), relative)
        for prefix in PREFIX_PATTERN.findall(text):
            if "Template_" in prefix:
                continue
            prefixes.setdefault(prefix.lower(), relative)
    return uids, prefixes


def template_help(cwd: Path) -> str:
    try:
        completed = subprocess.run(
            ["dotnet", "new", "XNCF", "--help"],
            cwd=cwd,
            check=False,
            capture_output=True,
            text=True,
        )
    except FileNotFoundError:
        return ""
    return (completed.stdout or "") + "\n" + (completed.stderr or "")


def template_help_recognized(help_text: str) -> bool:
    return "--XncfName" in help_text and "--OrgName" in help_text


def supported(help_text: str, switch: str) -> bool:
    if not template_help_recognized(help_text):
        return True
    return switch in help_text


def resolve_packages(web_project: Path | None) -> tuple[dict[str, str], dict[str, str], dict[str, str]]:
    host_versions: dict[str, str] = {}
    if web_project is not None:
        host_versions.update(package_references(read_text(web_project)))
        assets = web_project.parent / "obj" / "project.assets.json"
        for name, version in versions_from_assets(assets).items():
            host_versions.setdefault(name, version)

    packages: dict[str, str] = {}
    sources: dict[str, str] = {}
    for package_id in CORE_PACKAGES:
        if package_id in host_versions:
            packages[package_id] = host_versions[package_id]
            sources[package_id] = "host"
            continue
        latest = nuget_latest(package_id)
        if latest:
            packages[package_id] = latest
            sources[package_id] = "nuget"
    database_packages = {
        name: version
        for name, version in host_versions.items()
        if name.startswith("Senparc.Ncf.Database.")
    }
    return packages, sources, database_packages


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", default=".", help="NCF site root that contains Senparc.Web")
    parser.add_argument("--org", required=True, help="Organization identifier, for example Senparc")
    parser.add_argument("--name", required=True, help="Module identifier, for example Ordering")
    parser.add_argument("--output", help="Project directory. Default: sibling of Senparc.Web")
    parser.add_argument("--version", default="0.1.0")
    parser.add_argument("--menu-name")
    parser.add_argument("--icon", default="fa fa-cubes")
    parser.add_argument("--description", required=True)
    parser.add_argument("--target-framework", help="Default: the host Senparc.Web target framework")
    parser.add_argument("--guid")
    parser.add_argument("--template-version", help="Senparc.Xncf.XncfBuilder.Template version. Default: latest on NuGet")
    parser.add_argument("--web", action="store_true")
    parser.add_argument("--database", action="store_true")
    parser.add_argument("--function", action="store_true")
    parser.add_argument("--webapi", action="store_true")
    parser.add_argument("--sample", action="store_true")
    parser.add_argument("--execute", action="store_true")
    parser.add_argument(
        "--install-template",
        action="store_true",
        help="Install Senparc.Xncf.XncfBuilder.Template from NuGet. --execute does this when the template is missing",
    )
    return parser


def main() -> int:
    args = build_parser().parse_args()
    root = Path(args.root).expanduser().resolve()
    if not root.is_dir():
        print(f"Not a directory: {root}", file=sys.stderr)
        return 2
    if not IDENTIFIER.fullmatch(args.org) or not IDENTIFIER.fullmatch(args.name):
        print("Org and name must match [A-Za-z][A-Za-z0-9]* so they are valid namespaces.", file=sys.stderr)
        return 2
    if not VERSION.fullmatch(args.version):
        print(f"Version is not a semantic version: {args.version}", file=sys.stderr)
        return 2

    project_name = f"{args.org}.Xncf.{args.name}"
    web_projects = find_web_projects(root)
    web_project = web_projects[0] if web_projects else None
    solution = find_solution(web_project) if web_project else None
    if args.output:
        output = Path(args.output).expanduser()
        output = output.resolve() if output.is_absolute() else (root / output).resolve()
    elif web_project is not None:
        output = (web_project.parent.parent / project_name).resolve()
    else:
        print("No Senparc.Web.csproj was found. Pass --output explicitly.", file=sys.stderr)
        return 2

    prefix = f"{args.org}_{args.name}_"
    guid = (args.guid or str(uuid.uuid4())).upper()
    if not re.fullmatch(r"[0-9A-F]{8}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{12}", guid):
        print("GUID must be an uppercase UUID.", file=sys.stderr)
        return 2
    if guid == ZERO_GUID:
        print("Refusing the zero GUID.", file=sys.stderr)
        return 2

    uids, prefixes = scan_identities(root)
    collisions: list[str] = []
    if guid in uids:
        collisions.append(f"UID {guid} already used by {uids[guid]}")
    existing_prefix = prefixes.get(prefix.lower())
    if existing_prefix:
        collisions.append(f"Database prefix {prefix} already used by {existing_prefix}")
    if output.exists():
        collisions.append(f"Output already exists: {output}")

    packages, package_sources, database_packages = resolve_packages(web_project)
    template_version = args.template_version or nuget_latest(TEMPLATE_PACKAGE)
    template_install = ["dotnet", "new", "install", TEMPLATE_PACKAGE if not template_version else f"{TEMPLATE_PACKAGE}::{template_version}", "--force"]
    help_text = template_help(root)
    template_ready = template_help_recognized(help_text)
    target_framework = args.target_framework
    if not target_framework and web_project is not None:
        match = TARGET_FRAMEWORK_PATTERN.search(strip_xml_comments(read_text(web_project)))
        target_framework = match.group(1).strip() if match else None
    target_framework = target_framework or "net10.0"

    command = [
        "dotnet", "new", "XNCF",
        "-n", project_name,
        "-o", str(output),
        "--OrgName", args.org,
        "--XncfName", args.name,
        "--Guid", guid,
        "--Version", args.version,
        "--MenuName", args.menu_name or args.name,
        "--Icon", args.icon,
        "--Description", args.description,
        "--TargetFramework", target_framework,
    ]
    optional_switches = {
        "--IntegrationToNcf": "true",
        "--XncfBaseVersion": packages.get("Senparc.Ncf.XncfBase"),
        "--NcfAreaBaseVersion": packages.get("Senparc.Ncf.AreaBase"),
    }
    for switch, value in optional_switches.items():
        if value and supported(help_text, switch):
            command.extend([switch, value])
    for enabled, switch in (
        (args.sample, "--Sample"),
        (args.function, "--Function"),
        (args.web, "--Web"),
        (args.database, "--Database"),
        (args.webapi, "--UseWebApi"),
    ):
        if enabled and supported(help_text, switch):
            command.extend([switch, "true"])

    plan = {
        "projectName": project_name,
        "output": str(output),
        "guid": guid,
        "databasePrefix": prefix,
        "version": args.version,
        "integrationProfile": "published-ncf",
        "dependencySource": "nuget",
        "host": {
            "webProject": str(web_project) if web_project else None,
            "solution": str(solution) if solution else None,
            "targetFramework": target_framework,
            "startupProject": str(web_project) if web_project else None,
        },
        "templatePackage": TEMPLATE_PACKAGE,
        "templateVersion": template_version,
        "templateInstalled": template_ready,
        "templateInstall": template_install,
        "packages": packages,
        "packageSources": package_sources,
        "hostDatabasePackages": database_packages,
        "command": command,
        "shellCommand": " ".join(quote(part) for part in command),
        "collisions": collisions,
        "notes": [
            "Install the template with templateInstall. Do not install a template from a source directory.",
            "After generation, replace ProjectReference entries that point outside this site with PackageReference entries from packages.",
            "Register.Version is replaced by --Version. Overwrite the generated csproj <Version> and its template release notes.",
            "Database=true still generates the Color sample. Replace it before shipping.",
            "Use host.startupProject as the dotnet ef startup project.",
        ],
    }
    if not database_packages:
        plan["notes"].append(
            "The host has no Senparc.Ncf.Database.* package reference. Confirm which database providers are installed before adding migrations."
        )
    print(json.dumps(plan, ensure_ascii=False, indent=2))
    sys.stdout.flush()

    if collisions:
        print("Plan has collisions. Do not generate this module.", file=sys.stderr)
        return 2
    if not args.execute and not args.install_template:
        return 0
    if not template_version and not template_ready:
        print("Could not resolve Senparc.Xncf.XncfBuilder.Template from NuGet.", file=sys.stderr)
        return 2

    if args.install_template or (args.execute and not template_ready):
        install = subprocess.run(template_install, cwd=root, check=False)
        if install.returncode != 0:
            return install.returncode
    if not args.execute:
        return 0
    created = subprocess.run(command, cwd=root, check=False)
    return created.returncode


if __name__ == "__main__":
    sys.exit(main())
