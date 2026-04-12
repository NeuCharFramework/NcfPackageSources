# -*- coding: utf-8 -*-
import re
import sys
from pathlib import Path

from deep_translator import GoogleTranslator

ROOT = Path(r"C:\Users\chnyo\Desktop\NcfPackageSources")
translator = GoogleTranslator(source="zh-CN", target="en")
ZH = re.compile(r"[\u4e00-\u9fff]")


def has_chinese(s: str) -> bool:
    return bool(ZH.search(s))


def split_fenced(text: str):
    parts = []
    i = 0
    n = len(text)
    while i < n:
        start = text.find("```", i)
        if start == -1:
            parts.append(("text", text[i:]))
            break
        if start > i:
            parts.append(("text", text[i:start]))
        nl = text.find("\n", start + 3)
        if nl == -1:
            parts.append(("text", text[start:]))
            break
        end = text.find("```", nl + 1)
        if end == -1:
            parts.append(("text", text[start:]))
            break
        parts.append(("code", text[start : end + 3]))
        i = end + 3
    return parts


def translate_text_chunk(s: str) -> str:
    s = s.replace("\r\n", "\n")
    if not s.strip() or not has_chinese(s):
        return s
    max_chunk = 4500
    out = []
    buf = []
    size = 0
    for line in s.split("\n"):
        llen = len(line) + 1
        if size + llen > max_chunk and buf:
            chunk = "\n".join(buf)
            try:
                out.append(translator.translate(chunk))
            except Exception as e:
                print("translate error:", e, file=sys.stderr)
                out.append(chunk)
            buf = [line]
            size = llen
        else:
            buf.append(line)
            size += llen
    if buf:
        chunk = "\n".join(buf)
        if has_chinese(chunk):
            try:
                out.append(translator.translate(chunk))
            except Exception as e:
                print("translate error:", e, file=sys.stderr)
                out.append(chunk)
        else:
            out.append(chunk)
    return "\n".join(out)


def translate_md(content: str) -> str:
    pieces = split_fenced(content)
    result = []
    for kind, seg in pieces:
        if kind == "code":
            result.append(seg)
        else:
            result.append(translate_text_chunk(seg))
    return "".join(result)


def link_line(cn_name: str) -> str:
    return f"[中文版]({cn_name})\n\n"


def process_pair(en_path: Path, cn_path: Path):
    if not cn_path.is_file():
        print("skip missing cn", cn_path)
        return
    cn = cn_path.read_text(encoding="utf-8")
    if cn_path.name == "SKILL.cn.md" and "multi-language-translation" in str(cn_path):
        print("skip skill multi-language (manual)")
        return
    body = translate_md(cn)
    first = link_line(cn_path.name)
    en_path.write_text(first + body.lstrip("\n"), encoding="utf-8")
    print("wrote", en_path.relative_to(ROOT))


def main():
    root_files = [
        "FUNCTIONS_MANAGEMENT_GUIDE",
        "IMPLEMENTATION_STEP1",
        "QUICK_REFERENCE",
        "README_IMPLEMENTATION_COMPLETE",
        "TASK1_COMPLETION_SUMMARY",
        "TASK2_ANALYSIS",
        "TASK_COMPLETION_SUMMARY",
        "EVENTBUS_CIRCULAR_REFERENCE_PROTECTION",
        "EVENTBUS_COMPLETE_SUMMARY",
        "EVENTBUS_FLOW_DIAGRAMS",
        "EVENTBUS_INSPECTION_REPORT",
        "EVENTBUS_QUICK_REFERENCE",
    ]
    for base in root_files:
        process_pair(ROOT / f"{base}.md", ROOT / f"{base}.cn.md")

    cur = ROOT / ".cursor"
    for name in ["TECHNICAL_CONSTRAINTS", "scratchpad"]:
        process_pair(cur / f"{name}.md", cur / f"{name}.cn.md")
    steps = cur / "steps"
    for p in sorted(steps.glob("step-*.cn.md")):
        en = p.with_name(p.name.replace(".cn.md", ".md"))
        process_pair(en, p)

    gui_root = ROOT / "tools" / "NcfDesktopApp.GUI"
    for p in sorted(gui_root.rglob("*.cn.md")):
        if "wwwroot\\lib\\echarts" in str(p) or "wwwroot/lib/echarts" in str(p):
            continue
        en = p.with_name(p.name.replace(".cn.md", ".md"))
        process_pair(en, p)

    ns = ROOT / "tools" / "NcfSimulatedSite"
    for p in sorted(ns.rglob("readme.cn.md")):
        en = p.with_name("readme.md")
        process_pair(en, p)


if __name__ == "__main__":
    main()
