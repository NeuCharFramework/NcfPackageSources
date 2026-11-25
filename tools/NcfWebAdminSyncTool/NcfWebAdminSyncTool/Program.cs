/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Program.cs
    File description: Related implementation.

    Created by: Senparc - 20250321

    Modified by: Senparc - 20260726
    Change description: v0.0.1 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.0.2 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261124
    Change description: v0.0.3 UI localization (English default, optional Chinese)

    Modified by: Senparc - 20261124
    Change description: v0.0.3 Improved bilingual sync tool messages, language selection, and ignore handling.

----------------------------------------------------------------*/
using Microsoft.Extensions.Logging;

class Program
{
    private static readonly ILogger _logger;
    private static SyncToolMessages M = SyncToolMessages.English;
    private static readonly string[] IgnoredFolders = new[]
    {
        "bin", "obj", "SenparcTraceLog", "logs", ".git" ,".vs","NcfFiles"
    };
    private static readonly string[] IgnoredExtensions = new[]
    {
        ".csproj", ".user", ".DS_Store", ".Development.config"
    };
    //Legacy comment translated to English.
    private static readonly string[] IgnoredFileNames = new[]
    {
        "launchSettings.json", "NcfWebAdminBaseline.sln", "Dockerfile", "Dockerfile.original"
    };
    //Legacy comment translated to English.
    private static readonly string[] IgnoredFilePatterns = new[]
    {
        "-backup-"  //Legacy comment translated to English.
    };
    /// <summary>Relative path fragments skipped during baseline sync (module-specific App_Data, etc.).</summary>
    private static readonly string[] IgnoredRelativePathFragments = new[]
    {
        "App_Data/Senparc.Xncf.AIKernel",
        "App_Data/Senparc.Xncf.Installer",
    };
    //Legacy comment translated to English.
    private static ConsoleKey? _autoUpdateChoice = null;
    //Legacy comment translated to English.
    private static ConsoleKey? _autoOverwriteChoice = null;

    static Program()
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
        });
        _logger = loggerFactory.CreateLogger<Program>();
    }

    static async Task Main(string[] args)
    {
        try
        {
            var (lang, remainingArgs) = ParseLanguageArgument(args);
            if (lang == null)
            {
                if (Console.IsInputRedirected)
                {
                    M = SyncToolMessages.English;
                }
                else
                {
                    ResolveLanguageInteractively();
                }
            }
            else
            {
                M = lang.Value == UiLanguage.Chinese ? SyncToolMessages.Chinese : SyncToolMessages.English;
            }

            bool templateOnly = remainingArgs.Any(z =>
                z.Equals("--template-only", StringComparison.OrdinalIgnoreCase));
            bool nonInteractive = remainingArgs.Any(z =>
                z.Equals("--non-interactive", StringComparison.OrdinalIgnoreCase)
                || z.Equals("--yes", StringComparison.OrdinalIgnoreCase));
            if (nonInteractive)
            {
                _autoOverwriteChoice = ConsoleKey.Y;
                _autoUpdateChoice = ConsoleKey.U;
            }
            string sourceDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..","..", "..", "..", "..", "NcfWebAdminBaseline"));
            string targetDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..","..", "..", "..", "..", "..", "..", "NCF", "src", "back-end"));

            Console.WriteLine($"Source directory: {sourceDir}");
            Console.WriteLine(templateOnly
                ? "Mode: template-only (skip NcfWebAdminBaseline -> NCF back-end synchronization)"
                : $"Target directory: {targetDir}");

            //Legacy comment translated to English.
            bool hasError = false;
            if (!Directory.Exists(sourceDir))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"{M.ErrorSourceDirMissing} {sourceDir}");
                Console.ResetColor();
                hasError = true;
            }

            if (!templateOnly && !Directory.Exists(targetDir))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"{M.ErrorTargetDirMissing} {targetDir}");
                Console.ResetColor();
                hasError = true;
            }

            if (hasError)
            {
                Console.WriteLine(M.AbortEnsureDirsExist);
                return;
            }

            //Legacy comment translated to English.
            string templateSourceDir = Path.Combine(sourceDir, "Template_OrgName.Xncf.Template_XncfName");
            string templateTargetDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..","..", "..", "..", "..", "..", "src", "Extensions", "Senparc.Xncf.XncfBuilder", "Senparc.Xncf.XncfBuilder.Template", "templates", "template1"));

            if (Directory.Exists(templateSourceDir))
            {
                Console.WriteLine($"\nTemplate source directory: {templateSourceDir}");
                Console.WriteLine($"Template target directory: {templateTargetDir}");

                if (!Directory.Exists(templateTargetDir))
                {
                    Directory.CreateDirectory(templateTargetDir);
                    Console.WriteLine(M.CreatedTemplateTargetDir);
                }

                await SyncTemplateDirectoryAsync(templateSourceDir, templateTargetDir);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Template directory synchronization completed.");
                Console.ResetColor();
            }

            if (templateOnly)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Template-only synchronization completed successfully.");
                Console.ResetColor();
                return;
            }

            await SyncDirectoriesAsync(sourceDir, targetDir);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Synchronization completed successfully.");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: {ex.Message}");
            Console.ResetColor();
        }
    }

    static async Task SyncDirectoriesAsync(string sourceDir, string targetDir)
    {
        var sourceFiles = Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories)
            .Where(f => !ShouldIgnore(f))
            .ToList();

        foreach (var sourceFile in sourceFiles)
        {
            await SyncFileAsync(sourceFile, sourceDir, targetDir);
        }
    }

    static bool ShouldIgnore(string path)
    {
        var normalizedPath = path.Replace('\\', '/');
        if (IgnoredRelativePathFragments.Any(fragment =>
                normalizedPath.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
            return true;

        // checkdirectory
        var relativePath = Path.GetDirectoryName(path);
        if (IgnoredFolders.Any(folder => relativePath?.Contains(folder) ?? false))
            return true;

        //Legacy comment translated to English.
        var fileName = Path.GetFileName(path);

        //Legacy comment translated to English.
        if (IgnoredFileNames.Any(name => name.Equals(fileName, StringComparison.OrdinalIgnoreCase)))
            return true;

        //Legacy comment translated to English.
        var extension = Path.GetExtension(path);
        if (IgnoredExtensions.Any(ext =>
            ext.Equals(extension, StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
            return true;

        //Legacy comment translated to English.
        if (extension.Equals(".sln", StringComparison.OrdinalIgnoreCase))
        {
            if (IgnoredFilePatterns.Any(pattern => fileName.Contains(pattern, StringComparison.OrdinalIgnoreCase)))
                return true;
        }

        // checkhidefile
        if (fileName.StartsWith("."))
            return true;

        return false;
    }

    //Legacy comment translated to English.
    static async Task SyncTemplateDirectoryAsync(string sourceDir, string targetDir)
    {
        var sourceFiles = Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories)
            .Where(f => !ShouldIgnoreForTemplate(f))
            .ToList();

        Console.WriteLine($"\n{M.TemplateSyncHeader}");
        Console.WriteLine(string.Format(M.TemplateFilesFoundFormat, sourceFiles.Count));

        foreach (var sourceFile in sourceFiles)
        {
            await SyncTemplateFileAsync(sourceFile, sourceDir, targetDir);
        }
    }

    //Legacy comment translated to English.
    static bool ShouldIgnoreForTemplate(string path)
    {
        //Legacy comment translated to English.
        var fileName = Path.GetFileName(path);
        var extension = Path.GetExtension(path);

        //Legacy comment translated to English.
        var templateIgnoredFolders = new[] { "bin", "obj", ".vs" };
        var templateIgnoredExtensions = new[] { ".user", ".DS_Store" };

        // checkdirectory
        var relativePath = Path.GetDirectoryName(path);
        if (templateIgnoredFolders.Any(folder => relativePath?.Contains(folder) ?? false))
            return true;

        //Legacy comment translated to English.
        if (templateIgnoredExtensions.Any(ext =>
            ext.Equals(extension, StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
            return true;

        // checkhidefile
        if (fileName.StartsWith("."))
            return true;

        return false;
    }

    //Legacy comment translated to English.
    static async Task SyncTemplateFileAsync(string sourceFile, string sourceBaseDir, string targetBaseDir)
    {
        var relativePath = Path.GetRelativePath(sourceBaseDir, sourceFile);
        var targetFile = Path.Combine(targetBaseDir, relativePath);
        var targetDir = Path.GetDirectoryName(targetFile);

        if (!Directory.Exists(targetDir))
        {
            Directory.CreateDirectory(targetDir!);
        }

        bool shouldCopy = true;
        string? sourceContent = null;
        string? processedContent = null;

        //Legacy comment translated to English.
        if (Path.GetFileName(sourceFile).Equals("Register.cs", StringComparison.OrdinalIgnoreCase))
        {
            sourceContent = await File.ReadAllTextAsync(sourceFile);
            processedContent = sourceContent
                .Replace(
                    @"public override string Uid => ""C5852057-BF1D-492B-A22E-64F3C5F91D38"";",
                    @"public override string Uid => ""Template_Guid"";"
                )
                .Replace(
                    @"public override string Version => ""1.0.0"";",
                    @"public override string Version => ""Template_Version"";"
                );
        }

        if (File.Exists(targetFile))
        {
            bool contentSame = false;

            if (processedContent != null)
            {
                //Legacy comment translated to English.
                var targetContent = await File.ReadAllTextAsync(targetFile);
                contentSame = processedContent == targetContent;
            }
            else
            {
                //Legacy comment translated to English.
                var sourceFileInfo = new FileInfo(sourceFile);
                var targetFileInfo = new FileInfo(targetFile);

                if (sourceFileInfo.Length == targetFileInfo.Length)
                {
                    var sourceMD5 = await FileExtensions.CalculateMD5Async(sourceFile);
                    var targetMD5 = await FileExtensions.CalculateMD5Async(targetFile);
                    contentSame = sourceMD5 == targetMD5;
                }
            }

            if (contentSame)
            {
                //Legacy comment translated to English.
                var sourceTime = File.GetLastWriteTime(sourceFile);
                var targetTime = File.GetLastWriteTime(targetFile);

                if (sourceTime <= targetTime)
                {
                    //Legacy comment translated to English.
                    shouldCopy = false;
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine($"Template skipped (same): {relativePath}");
                    Console.ResetColor();
                }
                else
                {
                    //Legacy comment translated to English.
                    File.SetLastWriteTime(targetFile, sourceTime);
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Template timestamp updated: {relativePath}");
                    Console.ResetColor();
                    shouldCopy = false;
                }
            }
            else
            {
                //Legacy comment translated to English.
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"Template content differs, overwriting: {relativePath}");
                Console.ResetColor();
            }
        }

        if (shouldCopy)
        {
            try
            {
                if (processedContent != null)
                {
                    //Legacy comment translated to English.
                    await File.WriteAllTextAsync(targetFile, processedContent);
                }
                else
                {
                    //Legacy comment translated to English.
                    await FileExtensions.CopyAsync(sourceFile, targetFile, true);
                }

                File.SetLastWriteTime(targetFile, File.GetLastWriteTime(sourceFile));
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Template copied: {relativePath}");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error copying template {relativePath}: {ex.Message}");
                Console.ResetColor();
            }
        }
    }

    //Legacy comment translated to English.
    static List<(string Line, char Type)> GetDiff(string[] sourceLines, string[] targetLines)
    {
        var diff = new List<(string Line, char Type)>();

        //Legacy comment translated to English.
        int[,] lcs = new int[sourceLines.Length + 1, targetLines.Length + 1];
        for (int i = 1; i <= sourceLines.Length; i++)
        {
            for (int j = 1; j <= targetLines.Length; j++)
            {
                if (sourceLines[i - 1].Trim() == targetLines[j - 1].Trim())
                {
                    lcs[i, j] = lcs[i - 1, j - 1] + 1;
                }
                else
                {
                    lcs[i, j] = Math.Max(lcs[i - 1, j], lcs[i, j - 1]);
                }
            }
        }

        //Legacy comment translated to English.
        int sourceIndex = sourceLines.Length;
        int targetIndex = targetLines.Length;
        var tempDiff = new List<(string Line, char Type)>();

        while (sourceIndex > 0 || targetIndex > 0)
        {
            if (sourceIndex > 0 && targetIndex > 0 &&
                sourceLines[sourceIndex - 1].Trim() == targetLines[targetIndex - 1].Trim())
            {
                tempDiff.Add((sourceLines[sourceIndex - 1], ' '));
                sourceIndex--;
                targetIndex--;
            }
            else if (targetIndex > 0 &&
                    (sourceIndex == 0 || lcs[sourceIndex, targetIndex - 1] >= lcs[sourceIndex - 1, targetIndex]))
            {
                //Legacy comment translated to English.
                tempDiff.Add((targetLines[targetIndex - 1], '-'));
                targetIndex--;
            }
            else if (sourceIndex > 0 &&
                    (targetIndex == 0 || lcs[sourceIndex - 1, targetIndex] >= lcs[sourceIndex, targetIndex - 1]))
            {
                //Legacy comment translated to English.
                tempDiff.Add((sourceLines[sourceIndex - 1], '+'));
                sourceIndex--;
            }
        }

        tempDiff.Reverse();
        return tempDiff;
    }

    static void ShowFileDifference(string sourceFile, string targetFile, bool contentSame)
    {
        var sourceLines = File.ReadAllLines(sourceFile);
        var targetLines = File.ReadAllLines(targetFile);

        Console.WriteLine($"\n{M.DiffComparisonHeader}");
        Console.WriteLine("=".PadRight(50, '='));
        Console.WriteLine(string.Format(M.DiffSourceFileLabelFormat, Path.GetFileName(sourceFile)));
        Console.WriteLine(string.Format(M.DiffTargetFileLabelFormat, Path.GetFileName(targetFile)));
        Console.WriteLine("-".PadRight(50, '-'));

        var diff = GetDiff(sourceLines, targetLines);

        const int contextLines = 3;
        bool hasDiff = false;
        bool hasRealDiff = false;

        for (int k = 0; k < diff.Count; k++)
        {
            var (line, type) = diff[k];

            bool showLine = type != ' ' ||
                Enumerable.Range(Math.Max(0, k - contextLines), Math.Min(contextLines * 2, diff.Count - k))
                    .Any(idx => diff[idx].Type != ' ');

            if (!showLine) continue;

            if (type != ' ' && !string.IsNullOrWhiteSpace(line.Trim()))
            {
                hasRealDiff = true;
            }

            switch (type)
            {
                case '+':
                    //Legacy comment translated to English.
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"+ {line}");  //Legacy comment translated to English.
                    hasDiff = true;
                    break;
                case '-':
                    //Legacy comment translated to English.
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"- {line}");  //Legacy comment translated to English.
                    hasDiff = true;
                    break;
                default:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine($"  {line}");  //Legacy comment translated to English.
                    break;
            }
        }

        if (!hasDiff || !hasRealDiff)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(M.ContentSameTimestampOnly);
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(string.Format(M.ContentDiffersFormat, sourceFile));
            Console.ResetColor();
        }

        Console.ResetColor();
        Console.WriteLine("=".PadRight(50, '='));
    }

    static async Task SyncFileAsync(string sourceFile, string sourceBaseDir, string targetBaseDir)
    {
        var relativePath = Path.GetRelativePath(sourceBaseDir, sourceFile);
        var targetFile = Path.Combine(targetBaseDir, relativePath);
        var targetDir = Path.GetDirectoryName(targetFile);

        if (!Directory.Exists(targetDir))
        {
            Directory.CreateDirectory(targetDir!);
        }

        bool shouldCopy = true;
        bool isReverseUpdate = false;
        bool updateTimestampOnly = false;

        if (File.Exists(targetFile))
        {
            var sourceTime = File.GetLastWriteTime(sourceFile);
            var targetTime = File.GetLastWriteTime(targetFile);

            //Legacy comment translated to English.
            var sourceFileInfo = new FileInfo(sourceFile);
            var targetFileInfo = new FileInfo(targetFile);
            bool contentSame = false;

            if (sourceFileInfo.Length == targetFileInfo.Length)
            {
                //Legacy comment translated to English.
                var sourceMD5 = await FileExtensions.CalculateMD5Async(sourceFile);
                var targetMD5 = await FileExtensions.CalculateMD5Async(targetFile);
                contentSame = sourceMD5 == targetMD5;
            }

            if (contentSame)
            {
                //Legacy comment translated to English.
                if (targetTime > sourceTime)
                {
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Warning: Target file is newer than source file:");
                    Console.WriteLine($"Source: {relativePath} ({sourceTime})");
                    Console.WriteLine($"Target: {relativePath} ({targetTime})");
                    Console.WriteLine(M.ContentSameTimestampOnly);
                    Console.ResetColor();

                    if (_autoUpdateChoice == null)
                    {
                        Console.WriteLine(M.ChooseAction);
                        Console.WriteLine(M.OptionUpdateTimestamp);
                        Console.WriteLine(M.OptionSkipFile);
                        Console.WriteLine(M.OptionCoverAllTimestamp);
                        Console.WriteLine(M.OptionAutoApply);
                    }
                    else
                    {
                        var autoLabel = _autoUpdateChoice == ConsoleKey.U ? M.AutoUpdateTimestamp : M.AutoSkip;
                        Console.WriteLine(string.Format(M.AutoActionFormat, autoLabel));
                    }

                    if (_autoUpdateChoice != null)
                    {
                        shouldCopy = _autoUpdateChoice == ConsoleKey.U;
                        updateTimestampOnly = shouldCopy;
                        Console.WriteLine(shouldCopy ? M.AutoFileUpdate : M.AutoFileSkip);
                    }
                    else
                    {
                        while (true)
                        {
                            var response = Console.ReadKey();
                            Console.WriteLine();

                            switch (response.Key)
                            {
                                case ConsoleKey.U:
                                    shouldCopy = true;
                                    updateTimestampOnly = true;
                                    break;
                                case ConsoleKey.N:
                                    shouldCopy = false;
                                    break;
                                case ConsoleKey.C:
                                    _autoUpdateChoice = ConsoleKey.U;
                                    shouldCopy = true;
                                    updateTimestampOnly = true;
                                    Console.WriteLine(M.CoverAllTimestampSet);
                                    break;
                                case ConsoleKey.A:
                                    Console.Write(M.ChooseAutoActionPrompt);
                                    var autoChoice = Console.ReadKey();
                                    Console.WriteLine();

                                    if (autoChoice.Key == ConsoleKey.U || autoChoice.Key == ConsoleKey.N)
                                    {
                                        _autoUpdateChoice = autoChoice.Key;
                                        shouldCopy = autoChoice.Key == ConsoleKey.U;
                                        updateTimestampOnly = shouldCopy;
                                        var verb = shouldCopy ? M.AutoVerbUpdate : M.AutoVerbSkip;
                                        Console.WriteLine(string.Format(M.AutoSameContentSetFormat, verb));
                                        break;
                                    }
                                    continue;
                                default:
                                    Console.WriteLine(M.InvalidInputRetry);
                                    continue;
                            }
                            break;
                        }
                    }
                }
                else
                {
                    //Legacy comment translated to English.
                    shouldCopy = true;
                    updateTimestampOnly = true;
                    //Legacy comment translated to English.
                    File.SetLastWriteTime(targetFile, sourceTime);
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Updated timestamp: {relativePath}");
                    Console.ResetColor();
                    return; //Legacy comment translated to English.
                }
            }
            else
            {
                //Legacy comment translated to English.
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Warning: Files have different content:");
                Console.WriteLine($"Source: {relativePath} ({sourceTime})");
                Console.WriteLine($"Target: {relativePath} ({targetTime})");
                Console.ResetColor();

                ShowFileDifference(sourceFile, targetFile, false);

                if (_autoOverwriteChoice != null)
                {
                    var autoLabel = _autoOverwriteChoice switch
                    {
                        ConsoleKey.Y => M.AutoOverwriteSourceToTarget,
                        ConsoleKey.R => M.AutoOverwriteTargetToSource,
                        _ => M.AutoOverwriteSkip
                    };
                    Console.WriteLine(string.Format(M.AutoActionFormat, autoLabel));
                }
                else
                {
                    Console.WriteLine(M.ChooseAction);
                    Console.WriteLine(M.OptionOverwriteSourceToTarget);
                    Console.WriteLine(M.OptionOverwriteTargetToSource);
                    Console.WriteLine(M.OptionSkipFile);
                    Console.WriteLine(M.OptionCoverAllOverwrite);
                }

                if (_autoOverwriteChoice != null)
                {
                    shouldCopy = _autoOverwriteChoice != ConsoleKey.N;
                    isReverseUpdate = _autoOverwriteChoice == ConsoleKey.R;
                    var shortLabel = _autoOverwriteChoice switch
                    {
                        ConsoleKey.Y => M.AutoOverwriteSourceToTargetShort,
                        ConsoleKey.R => M.AutoOverwriteTargetToSourceShort,
                        _ => M.AutoOverwriteSkipShort
                    };
                    Console.WriteLine(string.Format(M.AutoOverwriteFileFormat, shortLabel));
                }
                else
                {
                    while (true)
                    {
                        var response = Console.ReadKey();
                        Console.WriteLine();

                        if (response.Key == ConsoleKey.Y)
                        {
                            shouldCopy = true;
                        }
                        else if (response.Key == ConsoleKey.R)
                        {
                            shouldCopy = true;
                            isReverseUpdate = true;
                        }
                        else if (response.Key == ConsoleKey.N)
                        {
                            shouldCopy = false;
                        }
                        else if (response.Key == ConsoleKey.C)
                        {
                            _autoOverwriteChoice = ConsoleKey.Y;
                            shouldCopy = true;
                            Console.WriteLine(M.CoverAllOverwriteSet);
                        }
                        else
                        {
                            Console.WriteLine(M.InvalidInputRetry);
                            continue;
                        }
                        break;
                    }
                }
            }
        }
        else
        {
            //Legacy comment translated to English.
            shouldCopy = true;
        }

        if (shouldCopy)
        {
            try
            {
                if (isReverseUpdate)
                {
                    //Legacy comment translated to English.
                    await FileExtensions.CopyAsync(targetFile, sourceFile, true);
                    File.SetLastWriteTime(sourceFile, File.GetLastWriteTime(targetFile));
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"Reverse updated: {relativePath} (target -> source)");
                    Console.ResetColor();
                }
                else if (updateTimestampOnly)
                {
                    //Legacy comment translated to English.
                    File.SetLastWriteTime(targetFile, File.GetLastWriteTime(sourceFile));
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Updated timestamp: {relativePath}");
                    Console.ResetColor();
                }
                else
                {
                    //Legacy comment translated to English.
                    await FileExtensions.CopyAsync(sourceFile, targetFile, true);
                    File.SetLastWriteTime(targetFile, File.GetLastWriteTime(sourceFile));
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Copied: {relativePath} (source -> target)");
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error copying {relativePath}: {ex.Message}");
                Console.ResetColor();
            }
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"Skipped: {relativePath}");
            Console.ResetColor();
        }
    }

    enum UiLanguage
    {
        English,
        Chinese
    }

    static (UiLanguage? Lang, string[] RemainingArgs) ParseLanguageArgument(string[] args)
    {
        var remaining = new List<string>(args.Length);
        UiLanguage? lang = null;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg.Equals("--lang", StringComparison.OrdinalIgnoreCase)
                || arg.Equals("-l", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 < args.Length)
                {
                    lang = ParseLanguageCode(args[++i]);
                }
                continue;
            }

            if (arg.StartsWith("--lang=", StringComparison.OrdinalIgnoreCase))
            {
                lang = ParseLanguageCode(arg[(arg.IndexOf('=') + 1)..]);
                continue;
            }

            remaining.Add(arg);
        }

        return (lang, remaining.ToArray());
    }

    static UiLanguage? ParseLanguageCode(string code)
    {
        if (code.Equals("en", StringComparison.OrdinalIgnoreCase)
            || code.Equals("eng", StringComparison.OrdinalIgnoreCase)
            || code.Equals("english", StringComparison.OrdinalIgnoreCase))
        {
            return UiLanguage.English;
        }

        if (code.Equals("zh", StringComparison.OrdinalIgnoreCase)
            || code.Equals("cn", StringComparison.OrdinalIgnoreCase)
            || code.Equals("chinese", StringComparison.OrdinalIgnoreCase))
        {
            return UiLanguage.Chinese;
        }

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"Unknown language code '{code}', using English.");
        Console.ResetColor();
        return UiLanguage.English;
    }

    static void ResolveLanguageInteractively()
    {
        // Language picker uses bilingual prompts so either audience can choose before M is set.
        Console.WriteLine(SyncToolMessages.English.LanguagePromptTitle);
        Console.WriteLine(SyncToolMessages.English.LanguageOptionEnglish);
        Console.WriteLine(SyncToolMessages.English.LanguageOptionChinese);
        Console.Write(SyncToolMessages.English.LanguageChoicePrompt);

        var key = Console.ReadKey(intercept: true);
        Console.WriteLine();

        if (key.Key == ConsoleKey.Z)
        {
            M = SyncToolMessages.Chinese;
            Console.WriteLine("UI language: 中文");
        }
        else if (key.Key == ConsoleKey.Enter || key.Key == ConsoleKey.E || key.Key == ConsoleKey.Spacebar)
        {
            M = SyncToolMessages.English;
            Console.WriteLine("UI language: English");
        }
        else
        {
            M = SyncToolMessages.English;
            Console.WriteLine($"UI language: English (unrecognized key '{key.KeyChar}', defaulting to English)");
        }
    }
}

// Extension method for File.Copy to support async operations
public static class FileExtensions
{
    public static async Task CopyAsync(string sourceFile, string destinationFile, bool overwrite)
    {
        using var sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var destinationStream = new FileStream(destinationFile, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
        await sourceStream.CopyToAsync(destinationStream);
    }

    public static async Task<string> CalculateMD5Async(string filePath)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        using var stream = File.OpenRead(filePath);
        var hash = await md5.ComputeHashAsync(stream);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}
