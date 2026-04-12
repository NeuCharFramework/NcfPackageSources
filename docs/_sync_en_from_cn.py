# -*- coding: utf-8 -*-
import re
import time
import pathlib
from deep_translator import GoogleTranslator

BASE = pathlib.Path(__file__).resolve().parent
TRANSLATOR = GoogleTranslator(source="zh-CN", target="en")
CJK = re.compile(r"[\u4e00-\u9fff]")
EN_LINK = re.compile(r"^\[English\]\([^)]+\)\s*$")


def split_fences(text: str) -> list[tuple[str, str]]:
    parts: list[tuple[str, str]] = []
    pos = 0
    n = len(text)
    while pos < n:
        i = text.find("```", pos)
        if i == -1:
            parts.append(("text", text[pos:]))
            break
        if i > pos:
            parts.append(("text", text[pos:i]))
        j = text.find("```", i + 3)
        if j == -1:
            parts.append(("text", text[i:]))
            break
        parts.append(("code", text[i : j + 3]))
        pos = j + 3
        if pos < n and text[pos] == "\r":
            pos += 1
        if pos < n and text[pos] == "\n":
            pos += 1
    return parts


def translate_plain(s: str) -> str:
    if not s.strip():
        return s
    max_len = 3500
    pieces: list[str] = []
    start = 0
    while start < len(s):
        end = min(start + max_len, len(s))
        if end < len(s):
            cut = s.rfind("\n\n", start, end)
            if cut > start + 500:
                end = cut + 2
            else:
                cut = s.rfind("\n", start, end)
                if cut > start + 500:
                    end = cut + 1
        chunk = s[start:end]
        for attempt in range(4):
            try:
                pieces.append(TRANSLATOR.translate(chunk))
                break
            except Exception:
                time.sleep(1.2 * (attempt + 1))
        else:
            pieces.append(chunk)
        start = end
        time.sleep(0.15)
    return "".join(pieces)


def translate_chunk(s: str) -> str:
    if not s.strip():
        return s
    parts = re.split(r"(`[^`\n]*`)", s)
    out: list[str] = []
    for p in parts:
        if len(p) >= 2 and p[0] == "`" and p[-1] == "`":
            out.append(p)
        elif CJK.search(p):
            out.append(translate_plain(p))
        else:
            out.append(p)
    return "".join(out)


def strip_en_link(text: str) -> str:
    lines = text.splitlines()
    if not lines:
        return text
    if EN_LINK.match(lines[0].strip()) or EN_LINK.match(lines[0]):
        lines = lines[1:]
        while lines and lines[0].strip() == "":
            lines = lines[1:]
        return "\n".join(lines) + ("\n" if text.endswith("\n") else "")
    return text


def build_english(cn_raw: str, cn_filename: str) -> str:
    body = strip_en_link(cn_raw)
    if body and not body.endswith("\n"):
        body += "\n"
    segments = split_fences(body)
    out_parts: list[str] = []
    for kind, seg in segments:
        if kind == "code":
            out_parts.append(seg)
        else:
            out_parts.append(translate_chunk(seg))
    body_en = "".join(out_parts)
    header = f"[中文版]({cn_filename})\n\n"
    return header + body_en.lstrip("\n")


def main() -> None:
    targets = [
        "Admin-Chat-API-Documentation.md",
        "Admin-Chat-Authentication.md",
        "Admin-Chat-Delivery-Report.md",
        "Admin-Chat-Drag-Troubleshooting.md",
        "Admin-Chat-Migration-Guide.md",
        "Agent-FunctionCalling-Architecture.md",
        "API-Authorization-Implementation.md",
        "bugfix-apihelper.md",
        "CLEANUP-SUMMARY.md",
        "EventBus-Handler-Registration-Fix.md",
        "KnowledgeBase-Embedding-Implementation.md",
        "PromptCatalyzer-404-Fix.md",
        "PromptCatalyzer-Auto-Shoot-And-Grade.md",
        "PromptCatalyzer-ChatGroup-Integration.md",
        "PromptCatalyzer-Complete-Fix-Summary.md",
        "PromptCatalyzer-Complete-Summary.md",
        "PromptCatalyzer-Complete-Testing-Guide.md",
        "PromptCatalyzer-Critical-Fixes.md",
        "PromptCatalyzer-Granular-Init-Fix.md",
        "PromptCatalyzer-Initialization-Complete-Guide.md",
        "PromptCatalyzer-Smart-Optimization.md",
        "PromptRange-Auto-Optimization-Guide.md",
        "PromptRange-JavaScript-Syntax-Fix.md",
        "README-REFACTORING.md",
        "REFACTOR-COMPLETE.md",
        "refactor-final-strategy.md",
    ]
    updated: list[str] = []
    for name in targets:
        stem = name[:-3]
        cn_name = f"{stem}.cn.md"
        cn_path = BASE / cn_name
        md_path = BASE / name
        if not cn_path.is_file():
            print("MISSING_CN", cn_name)
            continue
        cn_raw = cn_path.read_text(encoding="utf-8")
        print("PROCESS", name)
        en = build_english(cn_raw, cn_name)
        md_path.write_text(en, encoding="utf-8", newline="\n")
        updated.append(name)
        print("OK", name)
    print("UPDATED", len(updated), "files")


if __name__ == "__main__":
    main()
