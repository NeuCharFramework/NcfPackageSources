/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SyncToolMessages.cs
    File description: Localized console strings for NcfWebAdminSyncTool.

----------------------------------------------------------------*/

sealed class SyncToolMessages
{
    public required string ErrorSourceDirMissing { get; init; }
    public required string ErrorTargetDirMissing { get; init; }
    public required string AbortEnsureDirsExist { get; init; }
    public required string CreatedTemplateTargetDir { get; init; }
    public required string TemplateSyncHeader { get; init; }
    public required string TemplateFilesFoundFormat { get; init; }
    public required string DiffComparisonHeader { get; init; }
    public required string DiffSourceFileLabelFormat { get; init; }
    public required string DiffTargetFileLabelFormat { get; init; }
    public required string ContentSameTimestampOnly { get; init; }
    public required string ContentDiffersFormat { get; init; }
    public required string ChooseAction { get; init; }
    public required string OptionUpdateTimestamp { get; init; }
    public required string OptionSkipFile { get; init; }
    public required string OptionCoverAllTimestamp { get; init; }
    public required string OptionAutoApply { get; init; }
    public required string AutoActionFormat { get; init; }
    public required string AutoUpdateTimestamp { get; init; }
    public required string AutoSkip { get; init; }
    public required string AutoFileUpdate { get; init; }
    public required string AutoFileSkip { get; init; }
    public required string CoverAllTimestampSet { get; init; }
    public required string ChooseAutoActionPrompt { get; init; }
    public required string AutoVerbUpdate { get; init; }
    public required string AutoVerbSkip { get; init; }
    public required string AutoSameContentSetFormat { get; init; }
    public required string AutoOverwriteFileFormat { get; init; }
    public required string InvalidInputRetry { get; init; }
    public required string OptionOverwriteSourceToTarget { get; init; }
    public required string OptionOverwriteTargetToSource { get; init; }
    public required string OptionCoverAllOverwrite { get; init; }
    public required string AutoOverwriteSourceToTarget { get; init; }
    public required string AutoOverwriteTargetToSource { get; init; }
    public required string AutoOverwriteSkip { get; init; }
    public required string AutoOverwriteSourceToTargetShort { get; init; }
    public required string AutoOverwriteTargetToSourceShort { get; init; }
    public required string AutoOverwriteSkipShort { get; init; }
    public required string CoverAllOverwriteSet { get; init; }
    public required string LanguagePromptTitle { get; init; }
    public required string LanguageOptionEnglish { get; init; }
    public required string LanguageOptionChinese { get; init; }
    public required string LanguageChoicePrompt { get; init; }

    public static SyncToolMessages English { get; } = new()
    {
        ErrorSourceDirMissing = "Error: Source directory does not exist:",
        ErrorTargetDirMissing = "Error: Target directory does not exist:",
        AbortEnsureDirsExist = "Aborted: Ensure both source and target directories exist before running.",
        CreatedTemplateTargetDir = "Created template target directory.",
        TemplateSyncHeader = "Starting template directory sync (one-way: source -> target)",
        TemplateFilesFoundFormat = "Found {0} file(s) to process.",
        DiffComparisonHeader = "Difference comparison:",
        DiffSourceFileLabelFormat = "Source (new): {0}",
        DiffTargetFileLabelFormat = "Target (old): {0}",
        ContentSameTimestampOnly = "Content is identical; only last-write time differs.",
        ContentDiffersFormat = "Files differ: {0}",
        ChooseAction = "Choose an action:",
        OptionUpdateTimestamp = "U - Update timestamp",
        OptionSkipFile = "N - Skip this file",
        OptionCoverAllTimestamp = "C - Cover-All: update timestamp (U) for all similar cases",
        OptionAutoApply = "A - Auto-apply choice to all similar cases",
        AutoActionFormat = "Auto-applied action: {0}",
        AutoUpdateTimestamp = "update timestamp",
        AutoSkip = "skip",
        AutoFileUpdate = "Auto-updating file.",
        AutoFileSkip = "Auto-skipping file.",
        CoverAllTimestampSet = "Cover-All enabled: identical content with newer target will always update timestamp.",
        ChooseAutoActionPrompt = "Choose action to auto-apply (U/N): ",
        AutoVerbUpdate = "update",
        AutoVerbSkip = "skip",
        AutoSameContentSetFormat = "Auto-{0} enabled for identical-content files.",
        AutoOverwriteFileFormat = "Auto-{0} file.",
        InvalidInputRetry = "Invalid input, please try again.",
        OptionOverwriteSourceToTarget = "Y - Overwrite target with source",
        OptionOverwriteTargetToSource = "R - Overwrite source with target (reverse sync)",
        OptionCoverAllOverwrite = "C - Cover-All: overwrite with source (Y) for all similar cases",
        AutoOverwriteSourceToTarget = "Overwrite target with source",
        AutoOverwriteTargetToSource = "Overwrite source with target (reverse sync)",
        AutoOverwriteSkip = "Skip this file",
        AutoOverwriteSourceToTargetShort = "overwrite (source -> target)",
        AutoOverwriteTargetToSourceShort = "reverse sync (target -> source)",
        AutoOverwriteSkipShort = "skip",
        CoverAllOverwriteSet = "Cover-All enabled: differing content will always overwrite target with source.",
        LanguagePromptTitle = "Select UI language (default: English):",
        LanguageOptionEnglish = "  E - English",
        LanguageOptionChinese = "  Z - 中文 (Chinese)",
        LanguageChoicePrompt = "Choice [E]: ",
    };

    public static SyncToolMessages Chinese { get; } = new()
    {
        ErrorSourceDirMissing = "错误：源目录不存在：",
        ErrorTargetDirMissing = "错误：目标目录不存在：",
        AbortEnsureDirsExist = "程序终止：请确保源目录和目标目录都存在后再运行。",
        CreatedTemplateTargetDir = "创建模板目标目录",
        TemplateSyncHeader = "开始同步模板目录 (单向同步: 源 -> 目标)",
        TemplateFilesFoundFormat = "找到 {0} 个文件需要处理",
        DiffComparisonHeader = "差异对比：",
        DiffSourceFileLabelFormat = "源文件（新）: {0}",
        DiffTargetFileLabelFormat = "目标文件（旧）: {0}",
        ContentSameTimestampOnly = "文件内容相同，仅修改时间不同",
        ContentDiffersFormat = "文件内容存在差异: {0}",
        ChooseAction = "请选择操作：",
        OptionUpdateTimestamp = "U - 更新时间戳",
        OptionSkipFile = "N - 跳过此文件",
        OptionCoverAllTimestamp = "C - Cover-All：后续同类判断均更新时间戳 (U)",
        OptionAutoApply = "A - 自动执行后续操作（之后每次都执行相同选择）",
        AutoActionFormat = "自动执行操作：{0}",
        AutoUpdateTimestamp = "更新时间戳",
        AutoSkip = "跳过",
        AutoFileUpdate = "自动更新文件",
        AutoFileSkip = "自动跳过文件",
        CoverAllTimestampSet = "已设置 Cover-All：后续内容相同仅时间不同时均更新时间戳",
        ChooseAutoActionPrompt = "请选择要自动执行的操作 (U/N): ",
        AutoVerbUpdate = "更新",
        AutoVerbSkip = "跳过",
        AutoSameContentSetFormat = "已设置自动{0}后续内容相同的文件",
        AutoOverwriteFileFormat = "自动{0}文件",
        InvalidInputRetry = "无效的输入，请重试",
        OptionOverwriteSourceToTarget = "Y - 用源文件覆盖目标文件",
        OptionOverwriteTargetToSource = "R - 用目标文件覆盖源文件（反向更新）",
        OptionCoverAllOverwrite = "C - Cover-All：后续同类判断均用源覆盖 (Y)",
        AutoOverwriteSourceToTarget = "用源文件覆盖目标文件",
        AutoOverwriteTargetToSource = "用目标文件覆盖源文件（反向更新）",
        AutoOverwriteSkip = "跳过此文件",
        AutoOverwriteSourceToTargetShort = "覆盖 (源 -> 目标)",
        AutoOverwriteTargetToSourceShort = "反向更新 (目标 -> 源)",
        AutoOverwriteSkipShort = "跳过",
        CoverAllOverwriteSet = "已设置 Cover-All：后续内容不同均用源文件覆盖目标",
        LanguagePromptTitle = "选择界面语言（默认：English）:",
        LanguageOptionEnglish = "  E - English",
        LanguageOptionChinese = "  Z - 中文",
        LanguageChoicePrompt = "请选择 [E]: ",
    };
}