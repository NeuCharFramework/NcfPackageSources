using Microsoft.Extensions.Logging;

class Program
{
    private static readonly ILogger _logger;
    private static readonly string[] IgnoredFolders = new[] 
    { 
        "bin", "obj", "SenparcTraceLog", "logs", ".git" ,".vs"
    };
    private static readonly string[] IgnoredExtensions = new[] 
    { 
        ".csproj", ".user", ".DS_Store","SimulatedSite.sln"
    };
    // 新增忽略的文件名数组
    private static readonly string[] IgnoredFileNames = new[]
    {
        "launchSettings.json"
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
        var extension = Path.GetExtension(path);

        // 检查扩展名
        if (IgnoredExtensions.Any(ext => ext.Equals(extension, StringComparison.OrdinalIgnoreCase)))
            return true;

        // 检查完整文件名
        if (IgnoredFileNames.Any(name => name.Equals(fileName, StringComparison.OrdinalIgnoreCase)))
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
                tempDiff.Add((targetLines[targetIndex - 1], '+'));
                targetIndex--;
            }
            else if (sourceIndex > 0 && 
                    (targetIndex == 0 || lcs[sourceIndex - 1, targetIndex] >= lcs[sourceIndex, targetIndex - 1]))
            {
                tempDiff.Add((sourceLines[sourceIndex - 1], '-'));
                sourceIndex--;
            }
        }

        // 反转并返回结果
        tempDiff.Reverse();
        return tempDiff;
    }

    static void ShowFileDifference(string sourceFile, string targetFile, bool contentSame)
    {
        var sourceLines = File.ReadAllLines(sourceFile);
        var targetLines = File.ReadAllLines(targetFile);

        Console.WriteLine("\n差异对比：");
        Console.WriteLine("=".PadRight(50, '='));

        var diff = GetDiff(sourceLines, targetLines);
        
        // 显示差异，最多显示 3 行上下文
        const int contextLines = 3;
        bool hasDiff = false;
        bool hasRealDiff = false; // 用于检查是否有实际内容差异
        
        for (int k = 0; k < diff.Count; k++)
        {
            var (line, type) = diff[k];
            
            // 检查这行是否在任何差异的上下文范围内
            bool showLine = type != ' ' || 
                Enumerable.Range(Math.Max(0, k - contextLines), Math.Min(contextLines * 2, diff.Count - k))
                    .Any(idx => diff[idx].Type != ' ');

            if (!showLine) continue;

            // 检查是否存在实际内容差异（忽略空白字符）
            if (type != ' ' && !string.IsNullOrWhiteSpace(line.Trim()))
            {
                hasRealDiff = true;
            }

            switch (type)
            {
                case '-':
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"- {line}");
                    hasDiff = true;
                    break;
                case '+':
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"+ {line}");
                    hasDiff = true;
                    break;
                default:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine($"  {line}");
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

        if (File.Exists(targetFile))
        {
            var sourceTime = File.GetLastWriteTime(sourceFile);
            var targetTime = File.GetLastWriteTime(targetFile);

            if (targetTime > sourceTime)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Warning: Target file is newer than source file:");
                Console.WriteLine($"Source: {relativePath} ({sourceTime})");
                Console.WriteLine($"Target: {relativePath} ({targetTime})");
                Console.ResetColor();

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
                    // 如果 MD5 相同，直接显示内容相同的提示
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
                    Console.ResetColor();

                    if (_autoUpdateChoice != null)
                    {
                        shouldCopy = _autoUpdateChoice == ConsoleKey.U;
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
                    // 如果 MD5 不同，才进行详细的差异比较
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
        }

        if (shouldCopy)
        {
            try
            {
                if (isReverseUpdate)
                {
                    // 反向更新：将目标文件复制到源文件
                    await FileExtensions.CopyAsync(targetFile, sourceFile, true);
                    Console.ForegroundColor = ConsoleColor.Cyan; // 使用不同的颜色表示反向更新
                    Console.WriteLine($"Reverse updated: {relativePath} (target -> source)");
                    Console.ResetColor();
                }
                else
                {
                    // 正常更新：将源文件复制到目标文件
                    await FileExtensions.CopyAsync(sourceFile, targetFile, true);
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