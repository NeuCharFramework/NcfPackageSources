import os
import re
import sys
import time

from deep_translator import GoogleTranslator

CJK = re.compile(r"[\u4e00-\u9fff]")

_SEEDED = frozenset(
    p.replace("\\", "/")
    for p in [
        "src/Extensions/Senparc.Xncf.AgentsManager/OHS/readme.md",
        "src/Extensions/Senparc.Xncf.DynamicData/OHS/readme.md",
        "src/Extensions/Senparc.Xncf.FileManager/OHS/readme.md",
        "src/Extensions/Senparc.Xncf.KnowledgeBase/OHS/readme.md",
        "src/Extensions/Senparc.Xncf.MCP/OHS/readme.md",
        "src/Extensions/Senparc.Xncf.SenMapic/OHS/readme.md",
        "src/Extensions/Senparc.Xncf.XncfBuilder/Senparc.Xncf.XncfBuilder.Template/templates/template1/OHS/readme.md",
        "tools/NcfSimulatedSite/Template_OrgName.Xncf.Template_XncfName/OHS/readme.md",
        "src/Extensions/Senparc.Xncf.AgentsManager/OHS/Local/readme.md",
        "src/Extensions/Senparc.Xncf.DynamicData/OHS/Local/readme.md",
        "src/Extensions/Senparc.Xncf.FileManager/OHS/Local/readme.md",
        "src/Extensions/Senparc.Xncf.KnowledgeBase/OHS/Local/readme.md",
        "src/Extensions/Senparc.Xncf.MCP/OHS/Local/readme.md",
        "src/Extensions/Senparc.Xncf.SenMapic/OHS/Local/readme.md",
        "src/Extensions/Senparc.Xncf.XncfBuilder/Senparc.Xncf.XncfBuilder.Template/templates/template1/OHS/Local/readme.md",
        "tools/NcfSimulatedSite/Template_OrgName.Xncf.Template_XncfName/OHS/Local/readme.md",
        "src/Extensions/Senparc.Xncf.AgentsManager/OHS/Remote/readme.md",
        "src/Extensions/Senparc.Xncf.DynamicData/OHS/Remote/readme.md",
        "src/Extensions/Senparc.Xncf.FileManager/OHS/Remote/readme.md",
        "src/Extensions/Senparc.Xncf.KnowledgeBase/OHS/Remote/readme.md",
        "src/Extensions/Senparc.Xncf.MCP/OHS/Remote/readme.md",
        "src/Extensions/Senparc.Xncf.SenMapic/OHS/Remote/readme.md",
        "src/Extensions/Senparc.Xncf.XncfBuilder/Senparc.Xncf.XncfBuilder.Template/templates/template1/OHS/Remote/readme.md",
        "tools/NcfSimulatedSite/Template_OrgName.Xncf.Template_XncfName/OHS/Remote/readme.md",
        "src/Extensions/Senparc.Xncf.FileManager/Domain/readme.md",
        "src/Extensions/Senparc.Xncf.KnowledgeBase/Domain/readme.md",
        "src/Extensions/Senparc.Xncf.MCP/Domain/readme.md",
        "src/Extensions/Senparc.Xncf.SenMapic/Domain/readme.md",
        "src/Extensions/Senparc.Xncf.XncfBuilder/Senparc.Xncf.XncfBuilder.Template/templates/template1/Domain/readme.md",
        "tools/NcfSimulatedSite/Template_OrgName.Xncf.Template_XncfName/Domain/readme.md",
        "src/Extensions/Senparc.Xncf.AgentsManager/Domain/readme.md",
        "src/Extensions/Senparc.Xncf.DynamicData/Domain/readme.md",
        "src/Base/readme.md",
        "src/Basic/Senparc.Ncf.Core/EventBus/README.md",
        "src/Basic/Senparc.Ncf.Core/Models/readme.md",
        "src/Basic/Senparc.Ncf.Utility/DIExtension/readme.md",
        "src/Basic/Senparc.Ncf.DatabasePlant/readme.md",
    ]
)


def should_skip_dir(dirpath: str) -> bool:
    parts = dirpath.replace("\\", "/").split("/")
    if "node_modules" in parts or ".git" in parts:
        return True
    norm = dirpath.replace("\\", "/")
    if "wwwroot/lib/echarts" in norm:
        return True
    return False


def strip_english_link(text: str) -> str:
    lines = text.splitlines()
    if lines and lines[0].strip().startswith("[English]("):
        return "\n".join(lines[1:]).lstrip("\n")
    return text


def split_fenced(text: str) -> list[tuple[str, bool]]:
    parts = []
    i = 0
    while i < len(text):
        j = text.find("```", i)
        if j == -1:
            parts.append((text[i:], False))
            break
        if j > i:
            parts.append((text[i:j], False))
        k = text.find("```", j + 3)
        if k == -1:
            parts.append((text[j:], True))
            break
        parts.append((text[j : k + 3], True))
        i = k + 3
    return parts


def prose_has_cjk(text: str) -> bool:
    for seg, is_fence in split_fenced(text):
        if not is_fence and CJK.search(seg):
            return True
    return False


def skip_force_retranslate(rel_posix: str) -> bool:
    if rel_posix == "README.md":
        return True
    if rel_posix in _SEEDED:
        return True
    parts = rel_posix.split("/")
    if "ACL" in parts and rel_posix.endswith("readme.md"):
        return True
    return False


def translate_plain(text: str, translator: GoogleTranslator) -> str:
    if not CJK.search(text):
        return text
    max_len = 2000
    out = []
    start = 0
    while start < len(text):
        chunk = text[start : start + max_len]
        delay = 2.0
        for attempt in range(6):
            try:
                t = translator.translate(chunk)
                if t is None or not str(t).strip():
                    raise ValueError("empty translation")
                out.append(str(t))
                break
            except Exception as e:
                print(f"  err {e!r} attempt {attempt}", file=sys.stderr)
                time.sleep(delay)
                delay = min(delay * 1.8, 60)
        else:
            out.append(chunk)
        time.sleep(1.2)
        start += max_len
    return "".join(out)


def cn_basename(fname: str) -> str | None:
    if not fname.endswith(".md") or fname.endswith(".cn.md"):
        return None
    stem, _ = os.path.splitext(fname)
    if stem.endswith(".cn"):
        return None
    return stem + ".cn.md"


def main():
    dry = "--dry-run" in sys.argv
    force = "--force" in sys.argv
    path_filters: list[str] = []
    if "--" in sys.argv:
        path_filters = [x.replace("\\", "/") for x in sys.argv[sys.argv.index("--") + 1 :]]
    root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    os.chdir(root)
    translator = GoogleTranslator(source="zh-CN", target="en")
    done = 0
    skipped = 0
    for dirpath, _, files in os.walk("."):
        if should_skip_dir(dirpath):
            continue
        for fname in files:
            cn_name = cn_basename(fname)
            if not cn_name:
                continue
            cn_path = os.path.join(dirpath, cn_name)
            md_path = os.path.join(dirpath, fname)
            if not os.path.isfile(cn_path):
                continue
            main_text = None
            for enc in ("utf-8", "utf-8-sig", "gbk"):
                try:
                    main_text = open(md_path, encoding=enc).read()
                    break
                except (OSError, UnicodeDecodeError):
                    continue
            if main_text is None:
                continue
            rest = main_text
            lines = rest.splitlines()
            if lines and (
                lines[0].strip().startswith("[中文](")
                or lines[0].strip().startswith("[中文版](")
            ):
                rest = "\n".join(lines[1:]).lstrip("\n")
            rel = os.path.relpath(md_path, ".").replace("\\", "/")
            if path_filters and not any(f in rel for f in path_filters):
                continue
            if force and skip_force_retranslate(rel):
                skipped += 1
                continue
            if rel == "README.md":
                skipped += 1
                continue
            if not force and not prose_has_cjk(rest):
                skipped += 1
                continue
            cn_text = None
            for enc in ("utf-8", "utf-8-sig", "gbk"):
                try:
                    cn_text = open(cn_path, encoding=enc).read()
                    break
                except (OSError, UnicodeDecodeError):
                    continue
            if cn_text is None:
                continue
            source = strip_english_link(cn_text)
            parts = split_fenced(source)
            en_parts = []
            for i, (seg, is_fence) in enumerate(parts):
                piece = seg if is_fence else translate_plain(seg, translator)
                nxt = parts[i + 1] if i + 1 < len(parts) else None
                if not is_fence and nxt and nxt[1] and piece and not piece.endswith("\n"):
                    piece += "\n"
                if is_fence:
                    if en_parts and not en_parts[-1].endswith("\n"):
                        en_parts.append("\n")
                    en_parts.append(piece)
                    if nxt and not nxt[1] and not piece.endswith("\n"):
                        en_parts.append("\n")
                else:
                    en_parts.append(piece)
            body = "".join(en_parts).strip() + "\n"
            if not body.lstrip().startswith("[中文版]"):
                body = f"[中文版]({cn_name})\n\n{body}"
            if dry:
                print("translate", md_path)
                done += 1
                continue
            with open(md_path, "w", encoding="utf-8") as f:
                f.write(body)
            print(md_path, flush=True)
            done += 1
            time.sleep(2.0)
    print(f"done={done} skipped_already_en={skipped}", file=sys.stderr)


if __name__ == "__main__":
    main()
