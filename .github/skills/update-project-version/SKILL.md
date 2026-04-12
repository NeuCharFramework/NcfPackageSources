[中文版](SKILL.cn.md)

---
name: update-project-version
description: When the project file is modified (including .csproj file), help modify the corresponding .csproj content:
---

When the project file is modified (including .csproj file), help modify the corresponding .csproj content:
1. When there are multiple .csproj projects, use the one with the shortest file name.
2. When the <version> tag exists and the content is a qualified version number, increase the build version number by 1, otherwise no content of this file will be modified.
3. Add <PackageReleaseNotes>, enter today’s date and a summary of current project updates, in the following format:
[2025-11-01] update basic libraries, including Senparc.AI
