/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：WorkspaceFileSystemEngine.cs
    文件功能描述：Workspace Agent 受限文件系统与进程执行引擎

    创建标识：Senparc - 20261024
----------------------------------------------------------------*/

using System.ComponentModel;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Senparc.Xncf.Workspace.Abstractions;

namespace Senparc.Xncf.Workspace.Agent;

public sealed class WorkspaceFileSystemEngine
{
    private static readonly string[] ReservedRootEntries = [".workspace", ".workspace.json"];
    private readonly WorkspaceAgentOptions _options;
    private readonly string _rootPath;
    private readonly StringComparison _pathComparison;

    public WorkspaceFileSystemEngine(WorkspaceAgentOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
        _rootPath = Path.GetFullPath(options.RootPath);
        _pathComparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        Directory.CreateDirectory(_rootPath);
        EnsureWorkspaceRoot();
    }

    public string RootPath => _rootPath;

    public async Task<IReadOnlyList<WorkspaceEntry>> ListAsync(
        string relativePath,
        CancellationToken cancellationToken = default)
    {
        var directory = ResolvePath(relativePath, allowRoot: true);
        EnsureNoSymlink(directory);
        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException("Workspace directory does not exist.");
        }

        var entries = new List<WorkspaceEntry>();
        foreach (var path in Directory.EnumerateFileSystemEntries(directory)
                     .Where(path => !IsReservedPath(path))
                     .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                     .Take(Math.Max(1, _options.MaxEntriesPerList)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var attributes = File.GetAttributes(path);
            if (attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                continue;
            }

            var info = new FileInfo(path);
            entries.Add(new WorkspaceEntry(
                NormalizeRelativePath(path),
                attributes.HasFlag(FileAttributes.Directory),
                attributes.HasFlag(FileAttributes.Directory) ? 0 : info.Length,
                info.LastWriteTimeUtc));
        }

        await Task.CompletedTask.ConfigureAwait(false);
        return entries;
    }

    public async Task<WorkspaceFileMetadata> GetFileInfoAsync(
        string relativePath,
        CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(relativePath, allowRoot: true);
        EnsureNoSymlink(path);
        return await CreateMetadataAsync(path, cancellationToken).ConfigureAwait(false);
    }

    public async Task<WorkspaceFileContent> ReadFileAsync(
        string relativePath,
        int maxBytes,
        CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(relativePath);
        EnsureNoSymlink(path);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Workspace file does not exist.", relativePath);
        }

        var limit = Math.Min(Math.Max(1, maxBytes), Math.Max(1, _options.MaxFileReadBytes));
        var info = new FileInfo(path);
        if (info.Length > limit)
        {
            throw new InvalidOperationException($"Workspace file exceeds the {limit} byte read limit.");
        }

        var content = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
        var metadata = CreateFileMetadata(path, info, content);
        return new WorkspaceFileContent(metadata, content);
    }

    public async Task<WorkspaceWriteFileResult> WriteFileAsync(
        WorkspaceWriteFileRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Content);
        if (request.Content.LongLength > Math.Max(1, _options.MaxFileWriteBytes))
        {
            throw new InvalidOperationException(
                $"Workspace file exceeds the {_options.MaxFileWriteBytes} byte write limit.");
        }

        var path = ResolvePath(request.RelativePath);
        EnsureNoSymlink(path);
        var directory = Path.GetDirectoryName(path)
            ?? throw new InvalidOperationException("Workspace file has no parent directory.");
        EnsureNoSymlink(directory);
        var exists = File.Exists(path);
        if (!exists && !request.Create)
        {
            throw new FileNotFoundException("Workspace file does not exist.", request.RelativePath);
        }

        if (exists && !request.Overwrite)
        {
            throw new IOException("Workspace file already exists.");
        }

        if (exists && !string.IsNullOrWhiteSpace(request.ExpectedETag))
        {
            var current = await CreateMetadataAsync(path, cancellationToken).ConfigureAwait(false);
            if (!string.Equals(current.ETag, request.ExpectedETag, StringComparison.Ordinal))
            {
                throw new WorkspaceConflictException(
                    "Workspace file changed after it was read. Reload the file before saving.");
            }
        }

        var existingLength = exists ? new FileInfo(path).Length : 0;
        var currentBytes = GetWorkspaceBytes();
        if (currentBytes - existingLength + request.Content.LongLength > _options.MaxWorkspaceBytes)
        {
            throw new InvalidOperationException("Workspace quota exceeded.");
        }

        Directory.CreateDirectory(directory);
        var temporaryPath = Path.Combine(
            directory,
            $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.ncf-tmp");
        try
        {
            await File.WriteAllBytesAsync(temporaryPath, request.Content, cancellationToken).ConfigureAwait(false);
            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }

        return new WorkspaceWriteFileResult(
            await CreateMetadataAsync(path, cancellationToken).ConfigureAwait(false),
            !exists);
    }

    public Task CreateDirectoryAsync(
        string relativePath,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = ResolvePath(relativePath);
        EnsureNoSymlink(path);
        Directory.CreateDirectory(path);
        return Task.CompletedTask;
    }

    public async Task DeleteEntryAsync(
        WorkspaceDeleteEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        var path = ResolvePath(request.RelativePath);
        EnsureNoSymlink(path);
        if (File.Exists(path))
        {
            await EnsureExpectedETagAsync(path, request.ExpectedETag, cancellationToken).ConfigureAwait(false);
            File.Delete(path);
            return;
        }

        if (!Directory.Exists(path))
        {
            throw new FileNotFoundException("Workspace entry does not exist.", request.RelativePath);
        }

        if (!request.Recursive && Directory.EnumerateFileSystemEntries(path).Any())
        {
            throw new IOException("Workspace directory is not empty.");
        }

        Directory.Delete(path, request.Recursive);
    }

    public async Task MoveEntryAsync(
        WorkspaceMoveEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var source = ResolvePath(request.SourceRelativePath);
        var target = ResolvePath(request.TargetRelativePath);
        EnsureNoSymlink(source);
        EnsureNoSymlink(target);
        EnsureNoSymlink(Path.GetDirectoryName(target)
                        ?? throw new InvalidOperationException("Target has no parent directory."));
        var sourceIsFile = File.Exists(source);
        var sourceIsDirectory = Directory.Exists(source);
        if (!sourceIsFile && !sourceIsDirectory)
        {
            throw new FileNotFoundException("Workspace source entry does not exist.", request.SourceRelativePath);
        }

        if (sourceIsFile)
        {
            await EnsureExpectedETagAsync(source, request.ExpectedETag, cancellationToken).ConfigureAwait(false);
        }

        var targetExists = File.Exists(target) || Directory.Exists(target);
        if (targetExists && !request.Overwrite)
        {
            throw new IOException("Workspace target entry already exists.");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        if (targetExists)
        {
            if (File.Exists(target))
            {
                File.Delete(target);
            }
            else
            {
                Directory.Delete(target, recursive: true);
            }
        }

        if (sourceIsFile)
        {
            File.Move(source, target);
        }
        else
        {
            Directory.Move(source, target);
        }
    }

    public async Task<IReadOnlyList<WorkspaceSearchResult>> SearchAsync(
        WorkspaceSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.NameQuery) &&
            string.IsNullOrWhiteSpace(request.ContentQuery))
        {
            throw new ArgumentException("At least one search query is required.", nameof(request));
        }

        var searchRoot = ResolvePath(request.RelativePath, allowRoot: true);
        EnsureNoSymlink(searchRoot);
        if (!Directory.Exists(searchRoot))
        {
            throw new DirectoryNotFoundException("Workspace search directory does not exist.");
        }

        var comparison = request.CaseSensitive
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;
        var resultLimit = Math.Clamp(request.MaxResults, 1, Math.Max(1, _options.MaxSearchResults));
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
                if (IsReservedPath(path))
                {
                    continue;
                }

                var attributes = File.GetAttributes(path);
                if (attributes.HasFlag(FileAttributes.ReparsePoint))
                {
                    continue;
                }

                if (attributes.HasFlag(FileAttributes.Directory))
                {
                    var directoryName = Path.GetFileName(path);
                    if (!string.Equals(directoryName, ".git", comparison) &&
                        !string.Equals(directoryName, ".venv", comparison) &&
                        !string.Equals(directoryName, "node_modules", comparison))
                    {
                        pending.Push(path);
                    }

                    continue;
                }

                inspectedFiles++;
                var relativePath = NormalizeRelativePath(path);
                if (!string.IsNullOrWhiteSpace(request.NameQuery) &&
                    Path.GetFileName(path).Contains(request.NameQuery, comparison))
                {
                    results.Add(new WorkspaceSearchResult(
                        relativePath,
                        "fileName",
                        null,
                        Path.GetFileName(path)));
                    if (results.Count >= resultLimit)
                    {
                        break;
                    }
                }

                var fileInfo = new FileInfo(path);
                if (string.IsNullOrWhiteSpace(request.ContentQuery) ||
                    fileInfo.Length > _options.MaxSearchFileBytes ||
                    await IsBinaryFileAsync(path, cancellationToken).ConfigureAwait(false))
                {
                    continue;
                }

                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(stream, Encoding.UTF8, true);
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
        WorkspacePatchRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrEmpty(request.OldText))
        {
            throw new ArgumentException("Patch old text cannot be empty.", nameof(request));
        }

        var current = await ReadFileAsync(
            request.RelativePath,
            _options.MaxFileReadBytes,
            cancellationToken).ConfigureAwait(false);
        var currentText = Encoding.UTF8.GetString(current.Content);
        if (!string.IsNullOrWhiteSpace(request.ExpectedSha256))
        {
            var currentHash = Convert.ToHexString(SHA256.HashData(current.Content));
            if (!string.Equals(currentHash, request.ExpectedSha256, StringComparison.OrdinalIgnoreCase))
            {
                return new WorkspacePatchResult(
                    false,
                    0,
                    "补丁未应用：文件内容已变化，请重新读取文件后再生成补丁。");
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

        var updated = currentText.Replace(request.OldText, request.NewText, StringComparison.Ordinal);
        await WriteFileAsync(
            new WorkspaceWriteFileRequest(
                request.RelativePath,
                Encoding.UTF8.GetBytes(updated),
                ExpectedETag: current.Metadata.ETag),
            cancellationToken).ConfigureAwait(false);
        return new WorkspacePatchResult(true, replacementCount, "补丁应用成功。");
    }

    public Task<WorkspaceCommandResult> ExecuteCommandAsync(
        WorkspaceCommandRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureExecutionEnabled();
        var command = Path.GetFileName(request.Command?.Trim());
        if (string.IsNullOrWhiteSpace(command) ||
            !GetAllowedCommands().Any(item =>
                string.Equals(item, command, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("The requested command is not allowed by Workspace policy.");
        }

        var workingDirectory = ResolvePath(request.WorkingDirectory, allowRoot: true);
        EnsureNoSymlink(workingDirectory);
        if (!Directory.Exists(workingDirectory))
        {
            throw new DirectoryNotFoundException("Workspace working directory does not exist.");
        }

        return ExecuteProcessAsync(
            command,
            request.Arguments ?? Array.Empty<string>(),
            workingDirectory,
            cancellationToken);
    }

    public Task<WorkspaceCommandResult> ExecutePythonAsync(
        WorkspacePythonRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureExecutionEnabled();
        var scriptPath = ResolvePath(request.ScriptPath);
        EnsureNoSymlink(scriptPath);
        if (!File.Exists(scriptPath))
        {
            throw new FileNotFoundException("Python script does not exist.", request.ScriptPath);
        }

        if (!string.Equals(Path.GetExtension(scriptPath), ".py", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Python execution requires a .py script.");
        }

        var workingDirectory = ResolvePath(request.WorkingDirectory, allowRoot: true);
        EnsureNoSymlink(workingDirectory);
        if (!Directory.Exists(workingDirectory))
        {
            throw new DirectoryNotFoundException("Workspace working directory does not exist.");
        }

        var interpreter = ResolvePythonInterpreter();
        var arguments = new List<string>();
        if (interpreter.UsesLauncher)
        {
            arguments.Add("-3");
        }

        arguments.Add(scriptPath);
        arguments.AddRange(request.Arguments ?? Array.Empty<string>());
        return ExecuteProcessAsync(interpreter.FileName, arguments, workingDirectory, cancellationToken);
    }

    private async Task<WorkspaceCommandResult> ExecuteProcessAsync(
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
        PreserveSafeEnvironment(process.StartInfo);
        foreach (var argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument ?? string.Empty);
        }

        try
        {
            if (!process.Start())
            {
                throw new InvalidOperationException("Workspace command could not be started.");
            }
        }
        catch (Win32Exception exception)
        {
            return new WorkspaceCommandResult(-1, string.Empty, exception.Message, false);
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
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
            }
        }

        return new WorkspaceCommandResult(
            timedOut ? -1 : process.ExitCode,
            LimitOutput(await outputTask.ConfigureAwait(false)),
            LimitOutput(await errorTask.ConfigureAwait(false)),
            timedOut);
    }

    private async Task EnsureExpectedETagAsync(
        string path,
        string expectedETag,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(expectedETag))
        {
            return;
        }

        var metadata = await CreateMetadataAsync(path, cancellationToken).ConfigureAwait(false);
        if (!string.Equals(metadata.ETag, expectedETag, StringComparison.Ordinal))
        {
            throw new WorkspaceConflictException(
                "Workspace entry changed after it was read. Reload it before continuing.");
        }
    }

    private async Task<WorkspaceFileMetadata> CreateMetadataAsync(
        string path,
        CancellationToken cancellationToken)
    {
        if (Directory.Exists(path))
        {
            var directoryInfo = new DirectoryInfo(path);
            return new WorkspaceFileMetadata(
                NormalizeRelativePath(path),
                true,
                0,
                directoryInfo.CreationTimeUtc,
                directoryInfo.LastWriteTimeUtc,
                CreateDirectoryETag(directoryInfo));
        }

        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Workspace entry does not exist.", path);
        }

        var info = new FileInfo(path);
        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite,
            81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
        return new WorkspaceFileMetadata(
            NormalizeRelativePath(path),
            false,
            info.Length,
            info.CreationTimeUtc,
            info.LastWriteTimeUtc,
            $"\"{Convert.ToHexString(hash)}\"");
    }

    private WorkspaceFileMetadata CreateFileMetadata(string path, FileInfo info, byte[] content)
    {
        return new WorkspaceFileMetadata(
            NormalizeRelativePath(path),
            false,
            info.Length,
            info.CreationTimeUtc,
            info.LastWriteTimeUtc,
            $"\"{Convert.ToHexString(SHA256.HashData(content))}\"");
    }

    private static string CreateDirectoryETag(DirectoryInfo info)
    {
        var value = $"{info.CreationTimeUtc.Ticks}:{info.LastWriteTimeUtc.Ticks}";
        return $"\"{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))}\"";
    }

    private string ResolvePath(string? relativePath, bool allowRoot = false)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            if (allowRoot)
            {
                return _rootPath;
            }

            throw new InvalidOperationException("Workspace root cannot be modified as a file entry.");
        }

        if (Path.IsPathRooted(relativePath))
        {
            throw new InvalidOperationException("Workspace paths must be relative.");
        }

        var normalized = relativePath.Replace('\\', '/').TrimStart('/');
        if (normalized.Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Any(segment => segment is "." or ".."))
        {
            throw new InvalidOperationException("Workspace paths cannot contain traversal segments.");
        }

        var path = Path.GetFullPath(Path.Combine(_rootPath, normalized));
        var prefix = _rootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                     Path.DirectorySeparatorChar;
        if (!path.StartsWith(prefix, _pathComparison))
        {
            throw new InvalidOperationException("Workspace path escapes its root.");
        }

        if (IsReservedPath(path))
        {
            throw new UnauthorizedAccessException("Workspace internal metadata is not externally accessible.");
        }

        return path;
    }

    private void EnsureWorkspaceRoot()
    {
        if (File.GetAttributes(_rootPath).HasFlag(FileAttributes.ReparsePoint))
        {
            throw new InvalidOperationException("Workspace root cannot be a symbolic link.");
        }
    }

    private void EnsureNoSymlink(string path)
    {
        var relative = Path.GetRelativePath(_rootPath, path);
        var current = _rootPath;
        if (relative == ".")
        {
            return;
        }

        foreach (var segment in relative.Split(
                     [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
                     StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, segment);
            if ((File.Exists(current) || Directory.Exists(current)) &&
                File.GetAttributes(current).HasFlag(FileAttributes.ReparsePoint))
            {
                throw new InvalidOperationException("Workspace paths cannot traverse symbolic links.");
            }
        }
    }

    private bool IsReservedPath(string path)
    {
        var relative = NormalizeRelativePath(path);
        var firstSegment = relative.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return firstSegment != null &&
               ReservedRootEntries.Any(item =>
                   string.Equals(item, firstSegment, StringComparison.OrdinalIgnoreCase));
    }

    private string NormalizeRelativePath(string path)
    {
        var relative = Path.GetRelativePath(_rootPath, path);
        return relative == "."
            ? string.Empty
            : relative.Replace(Path.DirectorySeparatorChar, '/');
    }

    private long GetWorkspaceBytes()
    {
        var total = 0L;
        var pending = new Stack<string>();
        pending.Push(_rootPath);
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

    private IReadOnlyList<string> GetAllowedCommands()
    {
        return _options.AllowedCommands is { Count: > 0 }
            ? _options.AllowedCommands
            : Array.Empty<string>();
    }

    private (string FileName, bool UsesLauncher) ResolvePythonInterpreter()
    {
        var virtualEnvironmentPath = OperatingSystem.IsWindows()
            ? Path.Combine(_rootPath, ".venv", "Scripts", "python.exe")
            : Path.Combine(_rootPath, ".venv", "bin", "python");
        if (File.Exists(virtualEnvironmentPath) &&
            !File.GetAttributes(virtualEnvironmentPath).HasFlag(FileAttributes.ReparsePoint))
        {
            return (virtualEnvironmentPath, false);
        }

        var candidates = OperatingSystem.IsWindows()
            ? new[] { "py", "python" }
            : new[] { "python3", "python" };
        var interpreter = candidates.FirstOrDefault(candidate =>
            GetAllowedCommands().Any(allowed =>
                string.Equals(allowed, candidate, StringComparison.OrdinalIgnoreCase)));
        if (interpreter == null)
        {
            throw new InvalidOperationException("No allowed Python interpreter is configured.");
        }

        return (interpreter, string.Equals(interpreter, "py", StringComparison.OrdinalIgnoreCase));
    }

    private static void PreserveSafeEnvironment(ProcessStartInfo startInfo)
    {
        var safeNames = OperatingSystem.IsWindows()
            ? new[] { "PATH", "PATHEXT", "TEMP", "TMP", "SystemRoot", "COMSPEC", "DOTNET_ROOT" }
            : new[] { "PATH", "HOME", "TMPDIR", "LANG", "LC_ALL", "DOTNET_ROOT" };
        var safeValues = safeNames
            .Select(name => (Name: name, Value: Environment.GetEnvironmentVariable(name)))
            .Where(item => !string.IsNullOrEmpty(item.Value))
            .ToArray();
        startInfo.Environment.Clear();
        foreach (var (name, value) in safeValues)
        {
            startInfo.Environment[name] = value!;
        }

        startInfo.Environment["NCF_WORKSPACE_AGENT"] = "1";
    }

    private void EnsureExecutionEnabled()
    {
        if (!_options.ExecutionEnabled)
        {
            throw new UnauthorizedAccessException(
                "Code execution is disabled for this Workspace runtime. Use an isolated container runtime.");
        }
    }

    private string LimitOutput(string value)
    {
        var limit = Math.Max(1, _options.MaxCommandOutputBytes);
        return Encoding.UTF8.GetByteCount(value) <= limit
            ? value
            : value[..Math.Min(value.Length, limit)] + Environment.NewLine + "[output truncated]";
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

    private static async Task<bool> IsBinaryFileAsync(string path, CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];
        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite,
            buffer.Length,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        var count = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
        return buffer.AsSpan(0, count).Contains((byte)0);
    }
}
