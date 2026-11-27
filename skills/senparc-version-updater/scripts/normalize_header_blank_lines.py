#!/usr/bin/env python3
"""Collapse extra blank lines inside Senparc C# file header blocks."""
from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path


HEADER_START = re.compile(r"/\*-{2,}")
HEADER_END_MARKER = "----------------------------------------------------------------*/"


def normalize_header_block(block: str) -> tuple[str, bool]:
    """Keep at most one consecutive blank line inside the header block."""
    lines = block.split("\n")
    out: list[str] = []
    prev_empty = False
    changed = False
    for line in lines:
        empty = line.strip() == ""
        if empty:
            if prev_empty:
                changed = True
                continue
            prev_empty = True
            out.append("")
        else:
            prev_empty = False
            out.append(line)
    new_block = "\n".join(out)
    if new_block != block:
        changed = True
    return new_block, changed


def normalize_file(path: Path) -> bool:
    text = path.read_text(encoding="utf-8", errors="replace")
    start = text.find("/*----------------------------------------------------------------")
    end = text.find(HEADER_END_MARKER)
    if start < 0 or end < 0 or end < start:
        return False
    end += len(HEADER_END_MARKER)
    block = text[start:end]
    new_block, changed = normalize_header_block(block)
    if not changed:
        return False
    path.write_text(text[:start] + new_block + text[end:], encoding="utf-8")
    return True


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, required=True)
    parser.add_argument("--only-contains", default="", help="Only touch files containing this substring")
    args = parser.parse_args()
    root = args.root.resolve()
    count = 0
    for path in root.rglob("*.cs"):
        if "obj" in path.parts or "bin" in path.parts:
            continue
        try:
            text = path.read_text(encoding="utf-8", errors="replace")
        except OSError:
            continue
        if "/*----------------------------------------------------------------" not in text:
            continue
        if args.only_contains and args.only_contains not in text:
            continue
        if normalize_file(path):
            count += 1
    print(f"normalized {count} files", file=sys.stderr)
    return 0


if __name__ == "__main__":
    sys.exit(main())