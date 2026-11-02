---
name: update-project-version
description: 当项目文件修改时（包括.csproj文件），帮助修改对应 .csproj 的内容：
---

当项目文件修改时（包括.csproj文件），帮助修改对应 .csproj 的内容：
1. 当出现多个 .csproj 项目时，使用文件名最短的一个。
2. 当 <version> 标签存在，且内容是一个符合条件的版本号时，把build版本号+1，否则不修改这个文件的任何内容。
3. 追加<PackageReleaseNotes>，输入当天日期、当前项目更新的概要，格式如：
[2025-11-01] update basic libraries, including Senparc.AI
4. 检查并修正已有带日期的日志，按日期由旧到新排列，越新的日志越靠下；移动记录时保留其全部明细，同日记录保持原有相对顺序，新记录放在同日已有记录之后。保留无日期的历史记录，不臆造日期；无法确定其先后顺序时提示人工核对。
