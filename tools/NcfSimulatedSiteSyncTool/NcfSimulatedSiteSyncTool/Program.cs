using Microsoft.Extensions.Logging;

class Program
{
    private static readonly ILogger _logger;
    private static readonly string[] IgnoredFolders = new[] 
    { 
        "bin", "obj", "SenparcTraceLog", "logs", ".git" ,".vs","Template_OrgName.Xncf.Template_XncfName"
    };
    private static readonly string[] IgnoredExtensions = new[] 
    { 
        ".csproj", ".user", ".DS_Store", ".Development.config"
    };
    // 新增忽略的文件名数组
    private static readonly string[] IgnoredFileNames = new[]
    {
        "launchSettings.json", "NcfSimulatedSite.sln"
    };
    // 新增包含特定字符串的文件过滤规则
    private static readonly string[] IgnoredFilePatterns = new[]
    {
        "-backup-"  // 用于匹配包含 -backup- 的 .sln 文件
    };
    // 添加静态字段来存储自动选择的选项
    private static ConsoleKey? _autoUpdateChoice = null;

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
            string sourceDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..","..", "..", "..", "..", "NcfSimulatedSite"));
            string targetDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..","..", "..", "..", "..", "..", "..", "NCF", "src", "back-end"));

            Console.WriteLine($"Source directory: {sourceDir}");
            Console.WriteLine($"Target directory: {targetDir}");

            // 检查源目录和目标目录
            bool hasError = false;
            if (!Directory.Exists(sourceDir))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"错误：源目录不存在：{sourceDir}");
                Console.ResetColor();
                hasError = true;
            }

            if (!Directory.Exists(targetDir))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"错误：目标目录不存在：{targetDir}");
                Console.ResetColor();
                hasError = true;
            }

            if (hasError)
            {
                Console.WriteLine("程序终止：请确保源目录和目标目录都存在后再运行。");
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
        // 检查目录
        var relativePath = Path.GetDirectoryName(path);
        if (IgnoredFolders.Any(folder => relativePath?.Contains(folder) ?? false))
            return true;

        // 获取文件名和扩展名
        var fileName = Path.GetFileName(path);

        // 首先检查完整文件名
        if (IgnoredFileNames.Any(name => name.Equals(fileName, StringComparison.OrdinalIgnoreCase)))
            return true;

        // 检查扩展名
        var extension = Path.GetExtension(path);
        if (IgnoredExtensions.Any(ext => 
            ext.Equals(extension, StringComparison.OrdinalIgnoreCase) || 
            fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
            return true;

        // 检查 .sln 文件是否包含指定模式
        if (extension.Equals(".sln", StringComparison.OrdinalIgnoreCase))
        {
            if (IgnoredFilePatterns.Any(pattern => fileName.Contains(pattern, StringComparison.OrdinalIgnoreCase)))
                return true;
        }

        // 检查隐藏文件
        if (fileName.StartsWith("."))
            return true;

        return false;
    }

    // 添加 LCS (Longest Common Subsequence) 算法来找到真正的差异
    static List<(string Line, char Type)> GetDiff(string[] sourceLines, string[] targetLines)
    {
        var diff = new List<(string Line, char Type)>();
        
        // 创建 LCS 长度矩阵
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

        // 回溯找出差异
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
                // 目标文件（旧文件）中的行将被删除
                tempDiff.Add((targetLines[targetIndex - 1], '-'));
                targetIndex--;
            }
            else if (sourceIndex > 0 && 
                    (targetIndex == 0 || lcs[sourceIndex - 1, targetIndex] >= lcs[sourceIndex, targetIndex - 1]))
            {
                // 源文件（新文件）中的行将被添加
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

        Console.WriteLine("\n差异对比：");
        Console.WriteLine("=".PadRight(50, '='));
        Console.WriteLine($"源文件（新）: {Path.GetFileName(sourceFile)}");
        Console.WriteLine($"目标文件（旧）: {Path.GetFileName(targetFile)}");
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
                    // 源文件（新文件）中的行，显示为绿色的加号
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"+ {line}");  // 新增的行
                    hasDiff = true;
                    break;
                case '-':
                    // 目标文件（旧文件）中的行，显示为红色的减号
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"- {line}");  // 将被删除的行
                    hasDiff = true;
                    break;
                default:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine($"  {line}");  // 未变更的行
                    break;
            }
        }

        if (!hasDiff || !hasRealDiff)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("文件内容相同，仅修改时间不同");
            if (_autoUpdateChoice == null)
            {
                Console.WriteLine("请选择操作：");
                Console.WriteLine("U - 更新时间戳");
                Console.WriteLine("N - 跳过此文件");
                Console.WriteLine("A - 自动执行后续操作（之后每次都执行相同选择）");
            }
            else
            {
                Console.WriteLine($"自动执行操作：{(_autoUpdateChoice == ConsoleKey.U ? "更新时间戳" : "跳过")}");
            }
        }
        else
        {
            Console.WriteLine("\n文件内容存在差异");
            Console.WriteLine("请选择操作：");
            Console.WriteLine("Y - 用源文件覆盖目标文件");
            Console.WriteLine("R - 用目标文件覆盖源文件（反向更新）");
            Console.WriteLine("N - 跳过此文件");
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

            // 首先比较文件大小，如果大小不同则直接进行详细比较
            var sourceFileInfo = new FileInfo(sourceFile);
            var targetFileInfo = new FileInfo(targetFile);
            bool contentSame = false;

            if (sourceFileInfo.Length == targetFileInfo.Length)
            {
                // 如果文件大小相同，计算 MD5 进行比较
                var sourceMD5 = await FileExtensions.CalculateMD5Async(sourceFile);
                var targetMD5 = await FileExtensions.CalculateMD5Async(targetFile);
                contentSame = sourceMD5 == targetMD5;
            }

            if (contentSame)
            {
                // 如果内容相同，只更新时间戳
                if (targetTime > sourceTime)
                {
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Warning: Target file is newer than source file:");
                    Console.WriteLine($"Source: {relativePath} ({sourceTime})");
                    Console.WriteLine($"Target: {relativePath} ({targetTime})");
                    Console.WriteLine("文件内容相同，仅修改时间不同");
                    Console.ResetColor();

                    if (_autoUpdateChoice == null)
                    {
                        Console.WriteLine("请选择操作：");
                        Console.WriteLine("U - 更新时间戳");
                        Console.WriteLine("N - 跳过此文件");
                        Console.WriteLine("A - 自动执行后续操作（之后每次都执行相同选择）");
                    }
                    else
                    {
                        Console.WriteLine($"自动执行操作：{(_autoUpdateChoice == ConsoleKey.U ? "更新时间戳" : "跳过")}");
                    }

                    if (_autoUpdateChoice != null)
                    {
                        shouldCopy = _autoUpdateChoice == ConsoleKey.U;
                        updateTimestampOnly = shouldCopy;
                        Console.WriteLine($"自动{(shouldCopy ? "更新" : "跳过")}文件");
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
                                case ConsoleKey.A:
                                    Console.Write("请选择要自动执行的操作 (U/N): ");
                                    var autoChoice = Console.ReadKey();
                                    Console.WriteLine();
                                    
                                    if (autoChoice.Key == ConsoleKey.U || autoChoice.Key == ConsoleKey.N)
                                    {
                                        _autoUpdateChoice = autoChoice.Key;
                                        shouldCopy = autoChoice.Key == ConsoleKey.U;
                                        updateTimestampOnly = shouldCopy;
                                        Console.WriteLine($"已设置自动{(shouldCopy ? "更新" : "跳过")}后续内容相同的文件");
                                        break;
                                    }
                                    continue;
                                default:
                                    Console.WriteLine("无效的输入，请重试");
                                    continue;
                            }
                            break;
                        }
                    }
                }
                else
                {
                    // 如果目标文件时间更早，直接更新时间戳
                    shouldCopy = true;
                    updateTimestampOnly = true;
                    // 直接更新时间戳，不需要询问
                    File.SetLastWriteTime(targetFile, sourceTime);
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Updated timestamp: {relativePath}");
                    Console.ResetColor();
                    return; // 直接返回，不需要继续执行
                }
            }
            else
            {
                // 如果内容不同，显示差异并询问
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Warning: Files have different content:");
                Console.WriteLine($"Source: {relativePath} ({sourceTime})");
                Console.WriteLine($"Target: {relativePath} ({targetTime})");
                Console.ResetColor();

                ShowFileDifference(sourceFile, targetFile, false);
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
                    else
                    {
                        Console.WriteLine("无效的输入，请重试");
                        continue;
                    }
                    break;
                }
            }
        }
        else
        {
            // 目标文件不存在，直接复制
            shouldCopy = true;
        }

        if (shouldCopy)
        {
            try
            {
                if (isReverseUpdate)
                {
                    // 反向更新：将目标文件复制到源文件
                    await FileExtensions.CopyAsync(targetFile, sourceFile, true);
                    File.SetLastWriteTime(sourceFile, File.GetLastWriteTime(targetFile));
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"Reverse updated: {relativePath} (target -> source)");
                    Console.ResetColor();
                }
                else if (updateTimestampOnly)
                {
                    // 只更新时间戳
                    File.SetLastWriteTime(targetFile, File.GetLastWriteTime(sourceFile));
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Updated timestamp: {relativePath}");
                    Console.ResetColor();
                }
                else
                {
                    // 正常更新：将源文件复制到目标文件
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