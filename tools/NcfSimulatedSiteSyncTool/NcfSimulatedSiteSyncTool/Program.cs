using Microsoft.Extensions.Logging;

class Program
{
    private static readonly ILogger _logger;
    private static readonly string[] IgnoredFolders = new[] 
    { 
        "bin", "obj", "SenparcTraceLog", "logs", ".git" ,"Template_OrgName.Xncf.Template_XncfName"
    };
    private static readonly string[] IgnoredExtensions = new[] 
    { 
        ".csproj", ".user" ,".DS_Store","launchSettings.json",
    };

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
        var relativePath = Path.GetDirectoryName(path);
        if (IgnoredFolders.Any(folder => relativePath?.Contains(folder) ?? false))
            return true;

        var extension = Path.GetExtension(path);
        if (IgnoredExtensions.Any(ext => ext.Equals(extension, StringComparison.OrdinalIgnoreCase)))
            return true;

        var fileName = Path.GetFileName(path);
        if (fileName.StartsWith("."))
            return true;

        return false;
    }

    static void ShowFileDifference(string sourceFile, string targetFile)
    {
        var sourceLines = File.ReadAllLines(sourceFile);
        var targetLines = File.ReadAllLines(targetFile);

        Console.WriteLine("\n差异对比：");
        Console.WriteLine("=".PadRight(50, '='));

        var diff = new List<(string Line, char Type)>();
        int i = 0, j = 0;

        while (i < sourceLines.Length || j < targetLines.Length)
        {
            if (i >= sourceLines.Length)
            {
                // 目标文件有额外的行
                diff.Add((targetLines[j], '+'));
                j++;
            }
            else if (j >= targetLines.Length)
            {
                // 源文件有额外的行
                diff.Add((sourceLines[i], '-'));
                i++;
            }
            else if (sourceLines[i] == targetLines[j])
            {
                // 相同的行
                diff.Add((sourceLines[i], ' '));
                i++;
                j++;
            }
            else
            {
                // 不同的行
                diff.Add((sourceLines[i], '-'));
                diff.Add((targetLines[j], '+'));
                i++;
                j++;
            }
        }

        // 显示差异，最多显示 10 行上下文
        const int contextLines = 5;
        bool hasDiff = false;
        
        for (int k = 0; k < diff.Count; k++)
        {
            var (line, type) = diff[k];
            
            // 检查这行是否在任何差异的上下文范围内
            bool showLine = type != ' ' || 
                Enumerable.Range(Math.Max(0, k - contextLines), Math.Min(contextLines * 2, diff.Count - k))
                    .Any(idx => diff[idx].Type != ' ');

            if (!showLine) continue;

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

        if (!hasDiff)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("文件内容相同，仅修改时间不同");
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

                // 显示文件差异
                ShowFileDifference(sourceFile, targetFile);

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("Do you want to overwrite? (Y/N): ");
                Console.ResetColor();

                var response = Console.ReadKey();
                Console.WriteLine();
                shouldCopy = response.Key == ConsoleKey.Y;
            }
        }

        if (shouldCopy)
        {
            try
            {
                await FileExtensions.CopyAsync(sourceFile, targetFile, true);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Copied: {relativePath}");
                Console.ResetColor();
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
}