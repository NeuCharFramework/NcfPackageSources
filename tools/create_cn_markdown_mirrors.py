import os
import re
import sys

CJK = re.compile(r"[\u4e00-\u9fff]")
SKIP_DIR_PARTS = (".git", "node_modules", os.path.join("wwwroot", "lib", "echarts"))


def should_skip_dir(dirpath: str) -> bool:
    parts = dirpath.replace("\\", "/").split("/")
    if "node_modules" in parts:
        return True
    if ".git" in parts:
        return True
    norm = dirpath.replace("\\", "/")
    if "wwwroot/lib/echarts" in norm:
        return True
    return False


def main():
    dry = "--dry-run" in sys.argv
    created = 0
    for dirpath, _, files in os.walk("."):
        if should_skip_dir(dirpath):
            continue
        for fname in files:
            if not fname.endswith(".md") or fname.endswith(".cn.md"):
                continue
            path = os.path.join(dirpath, fname)
            text = None
            for enc in ("utf-8", "utf-8-sig", "gbk"):
                try:
                    text = open(path, encoding=enc).read()
                    break
                except (OSError, UnicodeDecodeError):
                    continue
            if text is None:
                continue
            if not CJK.search(text):
                continue
            stem, _ = os.path.splitext(fname)
            if stem.endswith(".cn"):
                continue
            cn_fname = stem + ".cn.md"
            cn_path = os.path.join(dirpath, cn_fname)
            if os.path.isfile(cn_path):
                continue
            body = text
            if not body.lstrip().startswith("[English]"):
                body = f"[English]({fname})\n\n{body}"
            if dry:
                print("create", cn_path)
                created += 1
                continue
            with open(cn_path, "w", encoding="utf-8") as f:
                f.write(body)
            created += 1
            print(cn_path)
    print(f"created {created} files", file=sys.stderr)


if __name__ == "__main__":
    main()
