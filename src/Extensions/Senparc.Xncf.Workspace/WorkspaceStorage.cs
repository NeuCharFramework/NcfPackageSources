using System.Text.Json;
using System.ComponentModel;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Senparc.Xncf.Workspace.Abstractions;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace Senparc.Xncf.Workspace;

public sealed class WorkspaceStorage : IWorkspaceStorage
{
    private const string ManifestName = ".workspace.json";
    private readonly WorkspaceOptions _options;
    private readonly string _rootPath;
    private readonly ILogger<WorkspaceStorage> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public WorkspaceStorage(
        IOptions<WorkspaceOptions> options,
        IHostEnvironment environment,
        ILogger<WorkspaceStorage> logger)
    {
        _options = options.Value;
        _rootPath = Path.GetFullPath(Path.IsPathRooted(_options.RootPath)
            ? _options.RootPath
            : Path.Combine(environment.ContentRootPath, _options.RootPath));
        Directory.CreateDirectory(_rootPath);
        _logger = logger;
    }

    public async Task<WorkspaceDescriptor> CreateAsync(
        WorkspaceCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.OwnerId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.OwnerId));
        }

        var name = NormalizeName(request.Name);
        var id = $"ws_{Guid.NewGuid():N}";
        var path = GetWorkspaceRoot(id);
        Directory.CreateDirectory(path);
        Directory.CreateDirectory(Path.Combine(path, "input"));
        Directory.CreateDirectory(Path.Combine(path, "output"));
        Directory.CreateDirectory(Path.Combine(path, "logs"));
        Directory.CreateDirectory(Path.Combine(path, ".workspace"));

        var now = DateTime.UtcNow;
        var descriptor = new WorkspaceDescriptor(id, request.OwnerId, name, path, now, now);
        await WriteManifestAsync(path, descriptor, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Workspace created: {WorkspaceId}, OwnerId={OwnerId}", id, request.OwnerId);
        return descriptor;
    }

    public async Task<WorkspaceDescriptor?> GetAsync(
        string workspaceId,
        CancellationToken cancellationToken = default)
    {
        var path = GetWorkspaceRoot(workspaceId);
        EnsureWorkspaceExists(path);
        var manifestPath = Path.Combine(path, ManifestName);
        if (!File.Exists(manifestPath))
        {
            return null;
        }

        await using var stream = File.OpenRead(manifestPath);
        return await JsonSerializer.DeserializeAsync<WorkspaceDescriptor>(
            stream, _jsonOptions, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<WorkspaceDescriptor>> ListWorkspacesAsync(
        CancellationToken cancellationToken = default)
    {
        var result = new List<WorkspaceDescriptor>();
        foreach (var directory in Directory.EnumerateDirectories(_rootPath, "ws_*"))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var descriptor = await GetAsync(Path.GetFileName(directory), cancellationToken).ConfigureAwait(false);
            if (descriptor != null)
            {
                result.Add(descriptor);
            }
        }

        return result
            .OrderByDescending(item => item.LastAccessedUtc)
            .ToList();
    }

    public async Task<IReadOnlyList<WorkspaceEntry>> ListAsync(
        string workspaceId,
        string relativePath = "",
        CancellationToken cancellationToken = default)
    {
        var workspaceRoot = GetWorkspaceRoot(workspaceId);
        EnsureWorkspaceExists(workspaceRoot);
        var directory = ResolveDirectory(workspaceRoot, relativePath);
        EnsureNoSymlink(workspaceRoot, directory);
        var entries = new List<WorkspaceEntry>();

        foreach (var path in Directory.EnumerateFileSystemEntries(directory)
                     .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                     .Take(_options.MaxEntriesPerList))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var info = new FileInfo(path);
            var relative = Path.GetRelativePath(workspaceRoot, path).Replace(Path.DirectorySeparatorChar, '/');
            entries.Add(info.Attributes.HasFlag(FileAttributes.Directory)
                ? new WorkspaceEntry(relative, true, 0, info.LastWriteTimeUtc)
                : new WorkspaceEntry(relative, false, info.Length, info.LastWriteTimeUtc));
        }

        return entries;
    }

    public Task<string> ResolveFilePathAsync(
        string workspaceId,
        string relativePath,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var workspaceRoot = GetWorkspaceRoot(workspaceId);
        EnsureWorkspaceExists(workspaceRoot);
        var path = ResolvePath(workspaceRoot, relativePath);
        EnsureNoSymlink(workspaceRoot, path);
        if (Directory.Exists(path))
        {
            throw new InvalidOperationException("Workspace path points to a directory.");
        }

        return Task.FromResult(path);
    }

    public async Task<string> ReadFileAsync(
        string workspaceId,
        string relativePath,
        int maxBytes = 2 * 1024 * 1024,
        CancellationToken cancellationToken = default)
    {
        var path = await ResolveFilePathAsync(workspaceId, relativePath, cancellationToken).ConfigureAwait(false);
        var limit = Math.Min(Math.Max(1, maxBytes), _options.MaxFileReadBytes);
        var info = new FileInfo(path);
        if (info.Length > limit)
        {
            throw new InvalidOperationException($"Workspace file exceeds the {limit} byte read limit.");
        }

        return await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
    }

    public async Task WriteFileAsync(
        string workspaceId,
        string relativePath,
        string content,
        CancellationToken cancellationToken = default)
    {
        if (content == null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        var bytes = Encoding.UTF8.GetByteCount(content);
        if (bytes > _options.MaxFileWriteBytes)
        {
            throw new InvalidOperationException($"Workspace file exceeds the {_options.MaxFileWriteBytes} byte write limit.");
        }

        var workspaceRoot = GetWorkspaceRoot(workspaceId);
        EnsureWorkspaceExists(workspaceRoot);
        var path = ResolvePath(workspaceRoot, relativePath);
        EnsureNoSymlink(workspaceRoot, path);
        var directory = Path.GetDirectoryName(path);
        if (directory == null || !directory.StartsWith(workspaceRoot, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Workspace path escapes its root.");
        }

        var existingLength = File.Exists(path) ? new FileInfo(path).Length : 0;
        var currentBytes = GetWorkspaceBytes(workspaceRoot);
        if (currentBytes - existingLength + bytes > _options.MaxWorkspaceBytes)
        {
            throw new InvalidOperationException("Workspace quota exceeded.");
        }

        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(path, content, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Workspace file written: {WorkspaceId}, RelativePath={RelativePath}", workspaceId, relativePath);
    }

    public Task CreateDirectoryAsync(
        string workspaceId,
        string relativePath,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var workspaceRoot = GetWorkspaceRoot(workspaceId);
        EnsureWorkspaceExists(workspaceRoot);
        var path = ResolvePath(workspaceRoot, relativePath);
        EnsureNoSymlink(workspaceRoot, path);
        Directory.CreateDirectory(path);
        _logger.LogInformation("Workspace directory created: {WorkspaceId}, RelativePath={RelativePath}", workspaceId, relativePath);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<WorkspaceSearchResult>> SearchAsync(
        string workspaceId,
        WorkspaceSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.NameQuery)
            && string.IsNullOrWhiteSpace(request.ContentQuery))
        {
            throw new ArgumentException("At least one search query is required.", nameof(request));
        }

        var workspaceRoot = GetWorkspaceRoot(workspaceId);
        EnsureWorkspaceExists(workspaceRoot);
        var searchRoot = ResolveDirectory(workspaceRoot, request.RelativePath);
        EnsureNoSymlink(workspaceRoot, searchRoot);
        var comparison = request.CaseSensitive
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;
        var resultLimit = Math.Clamp(request.MaxResults, 1, _options.MaxSearchResults);
        var fileLimit = Math.Max(1, _options.MaxSearchFiles);
        var results = new List<WorkspaceSearchResult>();
        var pending = new Stack<string>();
        pending.Push(searchRoot);
        var inspectedFiles = 0;

        while (pending.Count > 0 && results.Count < resultLimit && inspectedFiles < fileLimit)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var directory = pending.Pop();
            foreach (var path in Directory.EnumerateFileSystemEntries(directory)
                         .OrderBy(item => item, StringComparer.OrdinalIgnoreCase))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var attributes = File.GetAttributes(path);
                if (attributes.HasFlag(FileAttributes.ReparsePoint))
                {
                    continue;
                }

                if (Directory.Exists(path))
                {
                    var directoryName = Path.GetFileName(path);
                    if (!string.Equals(directoryName, ".git", comparison)
                        && !string.Equals(directoryName, ".venv", comparison)
                        && !string.Equals(directoryName, ".workspace", comparison))
                    {
                        pending.Push(path);
                    }

                    continue;
                }

                inspectedFiles++;
                var relativePath = Path.GetRelativePath(workspaceRoot, path)
                    .Replace(Path.DirectorySeparatorChar, '/');
                var fileNameMatch = !string.IsNullOrWhiteSpace(request.NameQuery)
                    && Path.GetFileName(path).Contains(request.NameQuery, comparison);
                if (fileNameMatch)
                {
                    results.Add(new WorkspaceSearchResult(relativePath, "fileName", null, Path.GetFileName(path)));
                    if (results.Count >= resultLimit)
                    {
                        break;
                    }
                }

                if (string.IsNullOrWhiteSpace(request.ContentQuery)
                    || new FileInfo(path).Length > _options.MaxSearchFileBytes)
                {
                    continue;
                }

                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                var lineNumber = 0;
                while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
                {
                    lineNumber++;
                    if (!line.Contains(request.ContentQuery, comparison))
                    {
                        continue;
                    }

                    results.Add(new WorkspaceSearchResult(
                        relativePath,
                        "content",
                        lineNumber,
                        line.Length <= 500 ? line : line[..500] + "…"));
                    if (results.Count >= resultLimit)
                    {
                        break;
                    }
                }
            }
        }

        return results;
    }

    public async Task<WorkspacePatchResult> ApplyPatchAsync(
        string workspaceId,
        WorkspacePatchRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrEmpty(request.OldText))
        {
            throw new ArgumentException("Patch old text cannot be empty.", nameof(request));
        }

        var currentText = await ReadFileAsync(
            workspaceId,
            request.RelativePath,
            _options.MaxFileReadBytes,
            cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(request.ExpectedSha256))
        {
            var currentHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(currentText)));
            if (!string.Equals(currentHash, request.ExpectedSha256, StringComparison.OrdinalIgnoreCase))
            {
                return new WorkspacePatchResult(false, 0, "补丁未应用：文件内容已变化，请重新读取文件后再生成补丁。");
            }
        }

        var replacementCount = CountOccurrences(currentText, request.OldText);
        var expectedReplacements = Math.Max(1, request.ExpectedReplacements);
        if (replacementCount != expectedReplacements)
        {
            return new WorkspacePatchResult(
                false,
                replacementCount,
                $"补丁未应用：找到 {replacementCount} 处匹配，但预期为 {expectedReplacements} 处。");
        }

        var updatedText = currentText.Replace(request.OldText, request.NewText, StringComparison.Ordinal);
        await WriteFileAsync(workspaceId, request.RelativePath, updatedText, cancellationToken).ConfigureAwait(false);
        return new WorkspacePatchResult(true, replacementCount, "补丁应用成功。");
    }

    public async Task<WorkspaceCommandResult> ExecuteCommandAsync(
        string workspaceId,
        WorkspaceCommandRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var workspaceRoot = GetWorkspaceRoot(workspaceId);
        EnsureWorkspaceExists(workspaceRoot);
        var command = Path.GetFileName(request.Command?.Trim());
        if (string.IsNullOrWhiteSpace(command)
            || !_options.AllowedCommands.Any(item => string.Equals(item, command, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("The requested command is not allowed by Workspace policy.");
        }

        var workingDirectory = ResolvePath(workspaceRoot, request.WorkingDirectory);
        EnsureNoSymlink(workspaceRoot, workingDirectory);
        if (!Directory.Exists(workingDirectory))
        {
            throw new DirectoryNotFoundException("Workspace working directory does not exist.");
        }

        return await ExecuteProcessAsync(
            workspaceId,
            command,
            request.Arguments ?? Array.Empty<string>(),
            workingDirectory,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<WorkspaceCommandResult> ExecutePythonAsync(
        string workspaceId,
        WorkspacePythonRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var workspaceRoot = GetWorkspaceRoot(workspaceId);
        EnsureWorkspaceExists(workspaceRoot);

        var scriptPath = ResolvePath(workspaceRoot, request.ScriptPath);
        EnsureNoSymlink(workspaceRoot, scriptPath);
        if (!File.Exists(scriptPath))
        {
            throw new FileNotFoundException("Python script does not exist.", request.ScriptPath);
        }

        if (!string.Equals(Path.GetExtension(scriptPath), ".py", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Python execution requires a .py script.");
        }

        var workingDirectory = ResolvePath(workspaceRoot, request.WorkingDirectory);
        EnsureNoSymlink(workspaceRoot, workingDirectory);
        if (!Directory.Exists(workingDirectory))
        {
            throw new DirectoryNotFoundException("Workspace working directory does not exist.");
        }

        var interpreter = ResolvePythonInterpreter(workspaceRoot);
        var arguments = new List<string>();
        if (interpreter.UsesLauncher)
        {
            arguments.Add(interpreter.Module);
        }

        arguments.Add(scriptPath);
        arguments.AddRange(request.Arguments ?? Array.Empty<string>());
        try
        {
            return await ExecuteProcessAsync(
                workspaceId,
                interpreter.FileName,
                arguments,
                workingDirectory,
                cancellationToken).ConfigureAwait(false);
        }
        catch (Win32Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Python interpreter could not be started: WorkspaceId={WorkspaceId}, Interpreter={Interpreter}",
                workspaceId,
                interpreter.FileName);
            return new WorkspaceCommandResult(
                -1,
                string.Empty,
                $"Python interpreter could not be started: {exception.Message}",
                false);
        }
    }

    public Task DeleteAsync(
        string workspaceId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = GetWorkspaceRoot(workspaceId);
        EnsureWorkspaceExists(path);
        Directory.Delete(path, recursive: true);
        _logger.LogWarning("Workspace deleted: {WorkspaceId}", workspaceId);
        return Task.CompletedTask;
    }

    private string GetWorkspaceRoot(string workspaceId)
    {
        if (string.IsNullOrWhiteSpace(workspaceId)
            || !workspaceId.StartsWith("ws_", StringComparison.Ordinal)
            || workspaceId.Length != 35
            || workspaceId.Any(character => !(char.IsAsciiLetterOrDigit(character) || character == '_')))
        {
            throw new ArgumentException("Invalid workspace identifier.", nameof(workspaceId));
        }

        return Path.Combine(_rootPath, workspaceId);
    }

    private static string NormalizeName(string name)
    {
        var normalized = string.IsNullOrWhiteSpace(name) ? "Workspace" : name.Trim();
        if (normalized.Length > 100)
        {
            normalized = normalized[..100];
        }

        return normalized;
    }

    private static string ResolveDirectory(string workspaceRoot, string relativePath)
    {
        var path = ResolvePath(workspaceRoot, relativePath);
        if (!Directory.Exists(path))
        {
            throw new DirectoryNotFoundException(path);
        }

        return path;
    }

    private static string ResolvePath(string workspaceRoot, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return workspaceRoot;
        }

        if (Path.IsPathRooted(relativePath))
        {
            throw new InvalidOperationException("Workspace paths must be relative.");
        }

        var normalized = relativePath.Replace('\\', '/');
        var path = Path.GetFullPath(Path.Combine(workspaceRoot, normalized));
        var prefix = workspaceRoot.EndsWith(Path.DirectorySeparatorChar)
            ? workspaceRoot
            : workspaceRoot + Path.DirectorySeparatorChar;
        if (!path.StartsWith(prefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Workspace path escapes its root.");
        }

        return path;
    }

    private static void EnsureWorkspaceExists(string path)
    {
        if (!Directory.Exists(path))
        {
            throw new DirectoryNotFoundException("Workspace does not exist.");
        }

        if (File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint))
        {
            throw new InvalidOperationException("Workspace root cannot be a symbolic link.");
        }
    }

    private static void EnsureNoSymlink(string workspaceRoot, string path)
    {
        var relative = Path.GetRelativePath(workspaceRoot, path);
        var current = workspaceRoot;
        if (relative == ".")
        {
            return;
        }

        foreach (var segment in relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
        {
            current = Path.Combine(current, segment);
            if ((File.Exists(current) || Directory.Exists(current))
                && File.GetAttributes(current).HasFlag(FileAttributes.ReparsePoint))
            {
                throw new InvalidOperationException("Workspace paths cannot traverse symbolic links.");
            }
        }
    }

    private string LimitOutput(string value)
    {
        var limit = Math.Max(1, _options.MaxCommandOutputBytes);
        return value.Length <= limit ? value : value[..limit] + Environment.NewLine + "[output truncated]";
    }

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }

    private (string FileName, bool UsesLauncher, string Module) ResolvePythonInterpreter(string workspaceRoot)
    {
        var virtualEnvironmentRoot = Path.Combine(workspaceRoot, ".venv");
        var virtualEnvironmentPath = OperatingSystem.IsWindows()
            ? Path.Combine(workspaceRoot, ".venv", "Scripts", "python.exe")
            : Path.Combine(workspaceRoot, ".venv", "bin", "python");
        if (Directory.Exists(virtualEnvironmentRoot)
            && !File.GetAttributes(virtualEnvironmentRoot).HasFlag(FileAttributes.ReparsePoint)
            && File.Exists(virtualEnvironmentPath)
            && !File.GetAttributes(virtualEnvironmentPath).HasFlag(FileAttributes.ReparsePoint))
        {
            return (virtualEnvironmentPath, false, string.Empty);
        }

        var candidates = OperatingSystem.IsWindows()
            ? new[] { "py", "python" }
            : new[] { "python3", "python" };
        var interpreter = candidates.FirstOrDefault(candidate =>
            _options.AllowedCommands.Any(allowed =>
                string.Equals(allowed, candidate, StringComparison.OrdinalIgnoreCase)));
        if (interpreter == null)
        {
            throw new InvalidOperationException("No allowed Python interpreter is configured for this Workspace.");
        }

        var usesLauncher = string.Equals(interpreter, "py", StringComparison.OrdinalIgnoreCase);
        return (interpreter, usesLauncher, usesLauncher ? "-3" : string.Empty);
    }

    private async Task<WorkspaceCommandResult> ExecuteProcessAsync(
        string workspaceId,
        string fileName,
        IReadOnlyList<string> arguments,
        string workingDirectory,
        CancellationToken cancellationToken)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        foreach (var argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument ?? string.Empty);
        }

        if (!process.Start())
        {
            throw new InvalidOperationException("Workspace command could not be started.");
        }

        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(_options.CommandTimeoutSeconds, 1, 600)));
        var timedOut = false;
        try
        {
            await process.WaitForExitAsync(timeoutCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            timedOut = true;
            try { process.Kill(entireProcessTree: true); } catch { }
        }

        var output = await outputTask.ConfigureAwait(false);
        var error = await errorTask.ConfigureAwait(false);
        _logger.LogInformation(
            "Workspace command completed: {WorkspaceId}, Command={Command}, ExitCode={ExitCode}, TimedOut={TimedOut}",
            workspaceId,
            fileName,
            timedOut ? -1 : process.ExitCode,
            timedOut);
        return new WorkspaceCommandResult(
            timedOut ? -1 : process.ExitCode,
            LimitOutput(output),
            LimitOutput(error),
            timedOut);
    }

    private static long GetWorkspaceBytes(string workspaceRoot)
    {
        var total = 0L;
        var pending = new Stack<string>();
        pending.Push(workspaceRoot);
        while (pending.Count > 0)
        {
            var directory = pending.Pop();
            foreach (var path in Directory.EnumerateFileSystemEntries(directory))
            {
                var attributes = File.GetAttributes(path);
                if (attributes.HasFlag(FileAttributes.ReparsePoint))
                {
                    continue;
                }

                if (attributes.HasFlag(FileAttributes.Directory))
                {
                    pending.Push(path);
                }
                else
                {
                    total = checked(total + new FileInfo(path).Length);
                }
            }
        }

        return total;
    }

    private async Task WriteManifestAsync(
        string path,
        WorkspaceDescriptor descriptor,
        CancellationToken cancellationToken)
    {
        var manifestPath = Path.Combine(path, ManifestName);
        await using var stream = File.Create(manifestPath);
        await JsonSerializer.SerializeAsync(stream, descriptor, _jsonOptions, cancellationToken)
            .ConfigureAwait(false);
    }
}
