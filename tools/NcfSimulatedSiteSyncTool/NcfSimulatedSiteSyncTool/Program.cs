using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.Text;

class Program
{
    private static readonly ILogger<Program> _logger;
    private static readonly string[] IgnoredFolders = new[]
    {
        "bin", "obj", ".git", "SenparcTraceLog", "logs", ".vs", ".idea", ".vscode"
    };

    private static readonly string[] IgnoredExtensions = new[]
    {
        ".csproj", ".user", ".suo", ".vspscc", ".vssscc", ".pdb", ".cache"
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
            var sourceDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "NcfSimulatedSite"));
            var targetDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "NCF", "src", "back-end"));

            if (!Directory.Exists(sourceDir) || !Directory.Exists(targetDir))
            {
                AnsiConsole.MarkupLine("[red]Error: Source or target directory not found![/]");
                return;
            }

            AnsiConsole.MarkupLine($"[green]Starting file synchronization...[/]");
            AnsiConsole.MarkupLine($"[yellow]Source: {sourceDir}[/]");
            AnsiConsole.MarkupLine($"[yellow]Target: {targetDir}[/]");

            await SyncDirectories(sourceDir, targetDir);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during synchronization");
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
        }
    }

    private static async Task SyncDirectories(string sourceDir, string targetDir)
    {
        var sourceFiles = Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories)
            .Where(f => !ShouldIgnoreFile(f))
            .ToList();

        foreach (var sourceFile in sourceFiles)
        {
            var relativePath = Path.GetRelativePath(sourceDir, sourceFile);
            var targetFile = Path.Combine(targetDir, relativePath);
            var targetDirPath = Path.GetDirectoryName(targetFile);

            if (targetDirPath != null && !Directory.Exists(targetDirPath))
            {
                Directory.CreateDirectory(targetDirPath);
            }

            if (File.Exists(targetFile))
            {
                var sourceInfo = new FileInfo(sourceFile);
                var targetInfo = new FileInfo(targetFile);

                if (sourceInfo.LastWriteTimeUtc > targetInfo.LastWriteTimeUtc)
                {
                    await CopyFile(sourceFile, targetFile);
                    AnsiConsole.MarkupLine($"[green]Updated: {relativePath}[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[yellow]Target file is newer: {relativePath}[/]");
                    AnsiConsole.MarkupLine($"[yellow]Source: {sourceInfo.LastWriteTimeUtc}[/]");
                    AnsiConsole.MarkupLine($"[yellow]Target: {targetInfo.LastWriteTimeUtc}[/]");

                    if (await AnsiConsole.Confirm("Do you want to overwrite the target file?"))
                    {
                        await CopyFile(sourceFile, targetFile);
                        AnsiConsole.MarkupLine($"[green]Overwritten: {relativePath}[/]");
                    }
                }
            }
            else
            {
                await CopyFile(sourceFile, targetFile);
                AnsiConsole.MarkupLine($"[blue]Created: {relativePath}[/]");
            }
        }
    }

    private static bool ShouldIgnoreFile(string filePath)
    {
        var path = filePath.ToLowerInvariant();
        return IgnoredFolders.Any(folder => path.Contains(Path.DirectorySeparatorChar + folder.ToLowerInvariant() + Path.DirectorySeparatorChar)) ||
               IgnoredExtensions.Any(ext => path.EndsWith(ext.ToLowerInvariant()));
    }

    private static async Task CopyFile(string sourceFile, string targetFile)
    {
        using var sourceStream = File.OpenRead(sourceFile);
        using var targetStream = File.Create(targetFile);
        await sourceStream.CopyToAsync(targetStream);
    }
}
