/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: WorkspaceFileSystemEngine.cs
    File description: Related implementation.

    Created by: Senparc - 20261024

    Modified by: Senparc - 20261103
    Change description: v0.2.1 implement connection manager and workspace file system provider

    Modified by: Senparc - 20261111
    Change description: provide file and directory candidates by relative path for use in the conversation input

    Modified by: Senparc - 20261111
    Change description: fix compatibility with the StartsWith overload used by relative-path glob validation

    Modified by: Senparc - 20261112
    Change description: v0.2.3 support path globs and content regex search, and improve error diagnostics

    Modified by: Senparc - 20261119
    Change description: v0.2.4 Improved Workspace Agent runtime diagnostics and packaging

----------------------------------------------------------------*/

using System.ComponentModel;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Senparc.Xncf.Workspace.Abstractions;

namespace Senparc.Xncf.WorkspaceAgent;

public sealed class WorkspaceFileSystemEngine
{
    private static readonly string[] ReservedRootEntries = [".workspace", ".workspace.json"];
    private readonly WorkspaceAgentOptions _options;
    private readonly string _rootPath;
    private readonly StringComparison _pathComparison;

    /// <summary>
    /// Initializes a file-system engine rooted at the configured workspace directory.
    /// The root is created and rejected when it is a symbolic link.
    /// </summary>
    /// <param name="options">Workspace root, quota, search, and execution options.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the workspace root is a symbolic link.</exception>
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

    /// <summary>
    /// Gets the normalized workspace root path.
    /// </summary>
    public string RootPath => _rootPath;

    /// <summary>
    /// Lists non-reserved files and directories below a relative path.
    /// </summary>
    /// <param name="relativePath">The relative directory path; the root is not accepted for file mutation but is valid for listing.</param>
    /// <param name="cancellationToken">Token used to cancel enumeration.</param>
    /// <returns>Visible entries ordered by relative path.</returns>
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

    /// <summary>
    /// Gets metadata and an ETag for a workspace file or directory.
    /// </summary>
    /// <param name="relativePath">The relative path to inspect.</param>
    /// <param name="cancellationToken">Token used to cancel metadata creation.</param>
    /// <returns>The current workspace metadata.</returns>
    public async Task<WorkspaceFileMetadata> GetFileInfoAsync(
        string relativePath,
        CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(relativePath, allowRoot: true);
        EnsureNoSymlink(path);
        return await CreateMetadataAsync(path, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Reads a file into memory after applying path, symlink, existence, and byte-limit checks.
    /// </summary>
    /// <param name="relativePath">The relative file path.</param>
    /// <param name="maxBytes">The caller-requested maximum byte count.</param>
    /// <param name="cancellationToken">Token used to cancel the read.</param>
    /// <returns>File bytes and metadata.</returns>
    /// <exception cref="WorkspaceLimitException">Thrown when the file exceeds the effective read limit.</exception>
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
            throw new WorkspaceLimitException($"Workspace file exceeds the {limit} byte read limit.");
        }

        var content = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
        var metadata = CreateFileMetadata(path, info, content);
        return new WorkspaceFileContent(metadata, content);
    }

    /// <summary>
    /// Reads a bounded line window without loading the entire text file into memory.
    /// </summary>
    /// <param name="request">The relative path, line range, and byte limit.</param>
    /// <param name="cancellationToken">Token used to cancel scanning.</param>
    /// <returns>The text window, continuation flag, truncation flag, and ETag.</returns>
    public async Task<WorkspaceTextWindow> ReadTextWindowAsync(
        WorkspaceTextWindowRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var path = ResolvePath(request.RelativePath);
        EnsureNoSymlink(path);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Workspace file does not exist.", request.RelativePath);
        }

        var startLine = Math.Max(1, request.StartLine);
        var lineCount = Math.Clamp(request.LineCount, 1, 2000);
        var maxBytes = Math.Clamp(request.MaxBytes, 1024, 512 * 1024);
        var info = new FileInfo(path);
        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        var scanner = new WorkspaceTextScanner(stream);
        var skipped = await scanner.SkipLinesAsync(startLine - 1, cancellationToken).ConfigureAwait(false);
        if (skipped < startLine - 1)
        {
            return new WorkspaceTextWindow(
                NormalizeRelativePath(path),
                info.Length,
                startLine,
                0,
                false,
                false,
                string.Empty,
                CreateWindowETag(info));
        }

        var builder = new StringBuilder();
        var returned = 0;
        var usedBytes = 0;
        var truncated = false;
        var hasMore = false;
        while (returned < lineCount)
        {
            var line = await scanner.ReadLineAsync(maxBytes, cancellationToken).ConfigureAwait(false);
            if (line == null)
            {
                break;
            }

            var piece = line.Value.Truncated
                ? Encoding.UTF8.GetString(line.Value.Bytes) + "…"
                : Encoding.UTF8.GetString(line.Value.Bytes);
            var additional = Encoding.UTF8.GetByteCount(piece) + (returned > 0 ? 1 : 0);
            if (returned > 0 && usedBytes + additional > maxBytes)
            {
                hasMore = true;
                break;
            }

            if (returned > 0)
            {
                builder.Append('\n');
            }

            builder.Append(piece);
            usedBytes += additional;
            returned++;
            if (line.Value.Truncated)
            {
                truncated = true;
                hasMore = await scanner.HasMoreAsync(cancellationToken).ConfigureAwait(false);
                break;
            }
        }

        if (!hasMore)
        {
            hasMore = await scanner.HasMoreAsync(cancellationToken).ConfigureAwait(false);
        }

        return new WorkspaceTextWindow(
            NormalizeRelativePath(path),
            info.Length,
            startLine,
            returned,
            hasMore,
            truncated,
            builder.ToString(),
            CreateWindowETag(info));
    }

    /// <summary>
    /// Writes a byte array using create, overwrite, ETag, path, symlink, and quota checks.
    /// </summary>
    /// <param name="request">The target path, content, and concurrency options.</param>
    /// <param name="cancellationToken">Token used to cancel the write.</param>
    /// <returns>Result metadata and whether a new file was created.</returns>
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

    /// <summary>
    /// Creates a relative directory after path and symlink checks.
    /// </summary>
    /// <param name="relativePath">The directory path relative to the workspace root.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
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

    /// <summary>
    /// Deletes a file or directory after optional ETag and recursive-deletion checks.
    /// </summary>
    /// <param name="request">The relative path and deletion options.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
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

    /// <summary>
    /// Moves a file or directory after source/target path, symlink, ETag, and overwrite checks.
    /// </summary>
    /// <param name="request">The source, target, overwrite, and expected ETag options.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
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

    /// <summary>
    /// Copies a file or directory after source/target path, symlink, ETag, overwrite,
    /// and workspace quota checks.
    /// </summary>
    /// <param name="request">The source, target, overwrite, and expected ETag options.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public async Task CopyEntryAsync(
        WorkspaceCopyEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

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

        if (PathsEqual(source, target))
        {
            throw new IOException("Workspace source and target paths must differ.");
        }

        if (sourceIsDirectory && IsDescendantPath(target, source))
        {
            throw new IOException("Workspace directory cannot be copied into itself.");
        }

        await EnsureExpectedETagAsync(source, request.ExpectedETag, cancellationToken).ConfigureAwait(false);

        var targetExists = File.Exists(target) || Directory.Exists(target);
        if (targetExists && !request.Overwrite)
        {
            throw new IOException("Workspace target entry already exists.");
        }

        var sourceBytes = GetEntryBytes(source, cancellationToken);
        var targetBytes = targetExists ? GetEntryBytes(target, cancellationToken) : 0;
        if (GetWorkspaceBytes() - targetBytes + sourceBytes > _options.MaxWorkspaceBytes)
        {
            throw new InvalidOperationException("Workspace quota exceeded.");
        }

        var parent = Path.GetDirectoryName(target)
            ?? throw new InvalidOperationException("Target has no parent directory.");
        Directory.CreateDirectory(parent);
        var temporaryPath = Path.Combine(
            parent,
            $".{Path.GetFileName(target)}.{Guid.NewGuid():N}.ncf-copy-tmp");
        try
        {
            if (sourceIsFile)
            {
                File.Copy(source, temporaryPath, overwrite: false);
            }
            else
            {
                CopyDirectory(source, temporaryPath, cancellationToken);
            }

            if (targetExists)
            {
                DeleteExistingPath(target);
            }

            if (sourceIsFile)
            {
                File.Move(temporaryPath, target);
            }
            else
            {
                Directory.Move(temporaryPath, target);
            }
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
            else if (Directory.Exists(temporaryPath))
            {
                Directory.Delete(temporaryPath, recursive: true);
            }
        }
    }

    /// <summary>
    /// Searches file names, relative paths, and bounded text content with optional glob/regex matching.
    /// </summary>
    /// <param name="request">The search terms, limits, case mode, and path/content filters.</param>
    /// <param name="cancellationToken">Token used to cancel traversal and reading.</param>
    /// <returns>Matching workspace results.</returns>
    public async Task<IReadOnlyList<WorkspaceSearchResult>> SearchAsync(
        WorkspaceSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.SuggestPaths)
        {
            return await SuggestPaths(request, cancellationToken).ConfigureAwait(false);
        }

        if (string.IsNullOrWhiteSpace(request.NameQuery) &&
            string.IsNullOrWhiteSpace(request.ContentQuery) &&
            string.IsNullOrWhiteSpace(request.PathGlob))
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
        var pathGlob = CreatePathGlobMatcher(request.PathGlob, request.CaseSensitive);
        var contentPattern = CreateContentMatcher(
            request.ContentQuery,
            request.ContentRegex,
            request.CaseSensitive);
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
                var nameMatches = string.IsNullOrWhiteSpace(request.NameQuery) ||
                    Path.GetFileName(path).Contains(request.NameQuery, comparison);
                var pathMatches = pathGlob == null || pathGlob.IsMatch(relativePath);
                if (nameMatches && pathMatches &&
                    (!string.IsNullOrWhiteSpace(request.NameQuery) || pathGlob != null))
                {
                    results.Add(new WorkspaceSearchResult(
                        relativePath,
                        pathGlob == null ? "fileName" : "path",
                        null,
                        Path.GetFileName(path)));
                    if (results.Count >= resultLimit)
                    {
                        break;
                    }
                }

                var fileInfo = new FileInfo(path);
                if (contentPattern == null ||
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
                    if (!contentPattern.IsMatch(line))
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

    /// <summary>
    /// Creates a bounded content regex matcher from a search request.
    /// </summary>
    /// <param name="query">The content query.</param>
    /// <param name="caseSensitive">Whether matching is case-sensitive.</param>
    /// <returns>A compiled matcher, or <see langword="null"/> for empty query.</returns>
    private static Regex? CreateContentMatcher(
        string query,
        bool useRegex,
        bool caseSensitive)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return null;
        }

        if (!useRegex)
        {
            var escapedQuery = Regex.Escape(query);
            return new Regex(
                escapedQuery,
                CreateSearchRegexOptions(caseSensitive),
                TimeSpan.FromSeconds(1));
        }

        try
        {
            return new Regex(
                query,
                CreateSearchRegexOptions(caseSensitive),
                TimeSpan.FromSeconds(1));
        }
        catch (ArgumentException exception)
        {
            throw new ArgumentException("Workspace content query is not a valid regular expression.", nameof(query), exception);
        }
    }

    /// <summary>
    /// Converts a relative path glob into a bounded regular-expression matcher.
    /// </summary>
    /// <param name="pathGlob">The glob pattern.</param>
    /// <param name="caseSensitive">Whether matching is case-sensitive.</param>
    /// <returns>A compiled path matcher, or <see langword="null"/> for empty input.</returns>
    private static Regex? CreatePathGlobMatcher(string pathGlob, bool caseSensitive)
    {
        if (string.IsNullOrWhiteSpace(pathGlob))
        {
            return null;
        }

        var normalized = pathGlob.Replace('\\', '/').Trim();
        if (normalized.Length > 500 ||
            normalized.StartsWith("/", StringComparison.Ordinal) ||
            normalized.IndexOfAny(['\0', '\r', '\n']) >= 0 ||
            normalized.Split('/').Any(segment => segment == ".."))
        {
            throw new ArgumentException(
                "Workspace path glob must be a relative path without parent traversal.",
                nameof(pathGlob));
        }

        var pattern = new StringBuilder("^");
        for (var index = 0; index < normalized.Length; index++)
        {
            var character = normalized[index];
            if (character == '*')
            {
                if (index + 1 < normalized.Length && normalized[index + 1] == '*')
                {
                    index++;
                    if (index + 1 < normalized.Length && normalized[index + 1] == '/')
                    {
                        pattern.Append("(?:.*/)?");
                        index++;
                    }
                    else
                    {
                        pattern.Append(".*");
                    }
                }
                else
                {
                    pattern.Append("[^/]*");
                }
            }
            else if (character == '?')
            {
                pattern.Append("[^/]");
            }
            else
            {
                pattern.Append(Regex.Escape(character.ToString()));
            }
        }

        pattern.Append('$');
        return new Regex(
            pattern.ToString(),
            CreateSearchRegexOptions(caseSensitive),
            TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// Creates the regex options used by workspace search matchers.
    /// </summary>
    /// <param name="caseSensitive">Whether matching is case-sensitive.</param>
    /// <returns>The configured regex options.</returns>
    private static RegexOptions CreateSearchRegexOptions(bool caseSensitive)
    {
        return RegexOptions.CultureInvariant
            | (caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// Suggests files and directories by relative-path query without reading file contents.
    /// </summary>
    /// <param name="request">The suggestion query and result limits.</param>
    /// <param name="cancellationToken">Token used to cancel traversal.</param>
    /// <returns>A task containing ranked path suggestions.</returns>
    private Task<IReadOnlyList<WorkspaceSearchResult>> SuggestPaths(
        WorkspaceSearchRequest request,
        CancellationToken cancellationToken)
    {
        var query = (request.NameQuery ?? string.Empty).Replace('\\', '/').Trim().TrimStart('/');
        if (query.Contains("..", StringComparison.Ordinal) || query.IndexOfAny(['\0', '\r', '\n']) >= 0)
        {
            throw new ArgumentException("Invalid Workspace path suggestion.", nameof(request));
        }

        var comparison = request.CaseSensitive
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;
        var resultLimit = Math.Clamp(request.MaxResults, 1, Math.Min(30, Math.Max(1, _options.MaxSearchResults)));
        var visitLimit = Math.Max(1, _options.MaxSearchFiles);
        var root = ResolvePath(request.RelativePath, allowRoot: true);
        EnsureNoSymlink(root);
        if (!Directory.Exists(root))
        {
            throw new DirectoryNotFoundException("Workspace search directory does not exist.");
        }

        var matches = new List<WorkspaceSearchResult>();
        var pending = new Stack<string>();
        pending.Push(root);
        var visited = 0;
        while (pending.Count > 0 && visited < visitLimit)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var directory = pending.Pop();
            foreach (var path in Directory.EnumerateFileSystemEntries(directory))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (IsReservedPath(path))
                {
                    continue;
                }

                if (++visited > visitLimit)
                {
                    break;
                }

                var attributes = File.GetAttributes(path);
                if (attributes.HasFlag(FileAttributes.ReparsePoint))
                {
                    continue;
                }

                var isDirectory = attributes.HasFlag(FileAttributes.Directory);
                var name = Path.GetFileName(path);
                if (isDirectory && IsHiddenWorkspaceDirectory(name))
                {
                    continue;
                }

                if (isDirectory && query.Length > 0)
                {
                    pending.Push(path);
                }

                var relativePath = NormalizeRelativePath(path);
                if (query.Length == 0)
                {
                    if (string.Equals(directory, root, _pathComparison))
                    {
                        matches.Add(CreatePathSuggestion(relativePath, isDirectory));
                    }

                    continue;
                }

                if (relativePath.Contains(query, comparison))
                {
                    matches.Add(CreatePathSuggestion(relativePath, isDirectory));
                }
            }
        }

        var ordered = matches
            .Select(item => (Item: item, Rank: RankPathSuggestion(item, query, comparison)))
            .OrderBy(item => item.Rank)
            .ThenBy(item => item.Item.RelativePath.Count(character => character == '/'))
            .ThenBy(item => item.Item.RelativePath, StringComparer.OrdinalIgnoreCase)
            .Take(resultLimit)
            .Select(item => item.Item)
            .ToArray();
        return Task.FromResult<IReadOnlyList<WorkspaceSearchResult>>(ordered);
    }

    /// <summary>
    /// Determines whether a directory is hidden from workspace suggestions.
    /// </summary>
    /// <param name="name">The directory name.</param>
    /// <returns><see langword="true"/> for hidden or reserved workspace directories.</returns>
    private static bool IsHiddenWorkspaceDirectory(string name)
    {
        return name.Equals(".git", StringComparison.OrdinalIgnoreCase)
            || name.Equals(".venv", StringComparison.OrdinalIgnoreCase)
            || name.Equals("node_modules", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Creates a path suggestion result with directory/file metadata.
    /// </summary>
    /// <param name="relativePath">The workspace-relative path.</param>
    /// <param name="isDirectory">Whether the path is a directory.</param>
    /// <returns>The suggestion result.</returns>
    private static WorkspaceSearchResult CreatePathSuggestion(string relativePath, bool isDirectory)
    {
        var name = Path.GetFileName(relativePath.TrimEnd('/'));
        return new WorkspaceSearchResult(relativePath, isDirectory ? "directory" : "path", null, name);
    }

    /// <summary>
    /// Calculates a relevance rank for a path suggestion.
    /// </summary>
    /// <param name="item">The suggestion item.</param>
    /// <param name="query">The normalized query.</param>
    /// <param name="comparison">The string comparison mode.</param>
    /// <returns>A higher score indicates a stronger match.</returns>
    private static int RankPathSuggestion(WorkspaceSearchResult item, string query, StringComparison comparison)
    {
        var isDirectory = item.MatchType == "directory";
        if (query.Length == 0)
        {
            return isDirectory ? 0 : 1;
        }

        var name = Path.GetFileName(item.RelativePath);
        if (name.Equals(query, comparison))
        {
            return isDirectory ? 1 : 0;
        }

        if (name.StartsWith(query, comparison))
        {
            return isDirectory ? 3 : 2;
        }

        if (item.RelativePath.Split('/').Any(segment => segment.StartsWith(query, comparison)))
        {
            return 4;
        }

        if (name.Contains(query, comparison))
        {
            return isDirectory ? 6 : 5;
        }

        return 7;
    }

    /// <summary>
    /// Applies an exact replacement after validating hash and expected match count, then writes atomically.
    /// </summary>
    /// <param name="request">The target file, old/new text, replacement count, and optional hash.</param>
    /// <param name="cancellationToken">Token used to cancel the patch.</param>
    /// <returns>The patch outcome and diagnostic message.</returns>
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
                    "Patch was not applied: the file content changed. Read the file again before generating a patch.");
            }
        }

        var replacementCount = CountOccurrences(currentText, request.OldText);
        var expectedReplacements = Math.Max(1, request.ExpectedReplacements);
        if (replacementCount != expectedReplacements)
        {
            return new WorkspacePatchResult(
                false,
                replacementCount,
                $"Patch was not applied: found {replacementCount} matching occurrences, but expected {expectedReplacements}.");
        }

        var updated = currentText.Replace(request.OldText, request.NewText, StringComparison.Ordinal);
        await WriteFileAsync(
            new WorkspaceWriteFileRequest(
                request.RelativePath,
                Encoding.UTF8.GetBytes(updated),
                ExpectedETag: current.Metadata.ETag),
            cancellationToken).ConfigureAwait(false);
        return new WorkspacePatchResult(true, replacementCount, "Patch applied successfully.");
    }

    /// <summary>
    /// Executes a command only when execution is enabled and the command is allow-listed.
    /// </summary>
    /// <param name="request">The command, arguments, and working directory.</param>
    /// <param name="cancellationToken">Token used to cancel process execution.</param>
    /// <returns>The bounded command result.</returns>
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

    /// <summary>
    /// Executes an allowed Python script only when execution is enabled.
    /// </summary>
    /// <param name="request">The script path, arguments, and working directory.</param>
    /// <param name="cancellationToken">Token used to cancel process execution.</param>
    /// <returns>The bounded command result.</returns>
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

    /// <summary>
    /// Starts an allowed workspace command with safe environment inheritance and bounded output.
    /// </summary>
    /// <param name="fileName">The executable name or launcher.</param>
    /// <param name="arguments">The command arguments.</param>
    /// <param name="workingDirectory">The validated working directory.</param>
    /// <param name="timeoutSeconds">The execution timeout.</param>
    /// <param name="cancellationToken">Token used to cancel the process.</param>
    /// <returns>A task containing exit code and bounded output.</returns>
    /// <exception cref="WorkspaceExecutionException">Thrown when execution is disabled or process startup fails.</exception>
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

    /// <summary>
    /// Verifies an optional file ETag for optimistic concurrency.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <param name="expectedETag">The caller's expected ETag.</param>
    /// <param name="cancellationToken">Token used to cancel metadata creation.</param>
    /// <exception cref="WorkspaceConflictException">Thrown when the current ETag differs.</exception>
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

    /// <summary>
    /// Creates metadata and an ETag for a file or directory.
    /// </summary>
    /// <param name="path">The file-system path.</param>
    /// <param name="cancellationToken">Token used for any metadata-dependent checks.</param>
    /// <returns>A task containing workspace metadata.</returns>
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

    /// <summary>
    /// Creates file metadata using the supplied file information and optional content bytes.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <param name="info">The file information.</param>
    /// <param name="content">Optional file content used for content-aware metadata.</param>
    /// <returns>The workspace metadata.</returns>
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

    /// <summary>Creates an ETag from directory timestamps and identity.</summary>
    /// <param name="info">The directory information.</param>
    /// <returns>The directory ETag.</returns>
    private static string CreateDirectoryETag(DirectoryInfo info)
    {
        var value = $"{info.CreationTimeUtc.Ticks}:{info.LastWriteTimeUtc.Ticks}";
        return $"\"{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))}\"";
    }

    /// <summary>Creates an ETag for a text-window read from file metadata.</summary>
    /// <param name="info">The file information.</param>
    /// <returns>The text-window ETag.</returns>
    private static string CreateWindowETag(FileInfo info)
    {
        return $"\"w-{info.Length:x}-{info.LastWriteTimeUtc.Ticks:x}\"";
    }

    /// <summary>
    /// Resolves a workspace-relative path under the configured root and rejects traversal.
    /// </summary>
    /// <param name="relativePath">The relative path.</param>
    /// <param name="allowRoot">Whether an empty path may resolve to the workspace root.</param>
    /// <returns>The normalized absolute path.</returns>
    /// <exception cref="WorkspacePathException">Thrown when the path escapes the workspace.</exception>
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

    /// <summary>
    /// Ensures the configured workspace root is not a symlink or reparse point.
    /// </summary>
    private void EnsureWorkspaceRoot()
    {
        if (File.GetAttributes(_rootPath).HasFlag(FileAttributes.ReparsePoint))
        {
            throw new InvalidOperationException("Workspace root cannot be a symbolic link.");
        }
    }

    /// <summary>
    /// Rejects reparse points on a workspace path and its existing parent chain.
    /// </summary>
    /// <param name="path">The path to validate.</param>
    /// <exception cref="WorkspacePathException">Thrown when a symbolic link or reparse point is encountered.</exception>
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

    /// <summary>Copies a directory tree while rejecting reparse points and checking cancellation.</summary>
    private static void CopyDirectory(
        string source,
        string target,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(target);
        foreach (var path in Directory.EnumerateFileSystemEntries(source))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var attributes = File.GetAttributes(path);
            if (attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                throw new InvalidOperationException("Workspace paths cannot copy symbolic links.");
            }

            var destination = Path.Combine(target, Path.GetFileName(path));
            if (attributes.HasFlag(FileAttributes.Directory))
            {
                CopyDirectory(path, destination, cancellationToken);
            }
            else
            {
                File.Copy(path, destination, overwrite: false);
            }
        }
    }

    /// <summary>Deletes an existing file or directory before an overwrite.</summary>
    private static void DeleteExistingPath(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        else if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }

    /// <summary>Calculates file bytes below one entry without following reparse points.</summary>
    private static long GetEntryBytes(string path, CancellationToken cancellationToken)
    {
        if (File.Exists(path))
        {
            return new FileInfo(path).Length;
        }

        var total = 0L;
        var pending = new Stack<string>();
        pending.Push(path);
        while (pending.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var directory = pending.Pop();
            foreach (var child in Directory.EnumerateFileSystemEntries(directory))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var attributes = File.GetAttributes(child);
                if (attributes.HasFlag(FileAttributes.ReparsePoint))
                {
                    throw new InvalidOperationException("Workspace paths cannot copy symbolic links.");
                }

                if (attributes.HasFlag(FileAttributes.Directory))
                {
                    pending.Push(child);
                }
                else
                {
                    total = checked(total + new FileInfo(child).Length);
                }
            }
        }

        return total;
    }

    /// <summary>Compares two normalized paths using the host file-system comparison rules.</summary>
    private bool PathsEqual(string left, string right) =>
        string.Equals(left, right, _pathComparison);

    /// <summary>Determines whether a path is strictly below a directory path.</summary>
    private bool IsDescendantPath(string path, string directory)
    {
        var prefix = directory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    + Path.DirectorySeparatorChar;
        return path.StartsWith(prefix, _pathComparison);
    }

    /// <summary>
    /// Determines whether a path is a reserved workspace metadata path.
    /// </summary>
    /// <param name="path">The path to inspect.</param>
    /// <returns><see langword="true"/> when the path is reserved.</returns>
    private bool IsReservedPath(string path)
    {
        var relative = NormalizeRelativePath(path);
        var firstSegment = relative.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return firstSegment != null &&
               ReservedRootEntries.Any(item =>
                   string.Equals(item, firstSegment, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Converts an absolute or relative workspace path into a normalized relative path.
    /// </summary>
    /// <param name="path">The path to normalize.</param>
    /// <returns>The normalized slash-separated relative path.</returns>
    private string NormalizeRelativePath(string path)
    {
        var relative = Path.GetRelativePath(_rootPath, path);
        return relative == "."
            ? string.Empty
            : relative.Replace(Path.DirectorySeparatorChar, '/');
    }

    /// <summary>
    /// Calculates the total workspace file size under the configured quota traversal rules.
    /// </summary>
    /// <returns>The total file bytes currently used.</returns>
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

    /// <summary>
    /// Gets the configured allow-list of executable commands.
    /// </summary>
    /// <returns>Allowed command names.</returns>
    private IReadOnlyList<string> GetAllowedCommands()
    {
        return _options.AllowedCommands is { Count: > 0 }
            ? _options.AllowedCommands
            : Array.Empty<string>();
    }

    /// <summary>
    /// Resolves the configured Python executable and whether it must run through a launcher.
    /// </summary>
    /// <returns>The executable name and launcher flag.</returns>
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

    /// <summary>
    /// Retains only the environment variables explicitly allowed for workspace processes.
    /// </summary>
    /// <param name="startInfo">The process-start configuration.</param>
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

    /// <summary>
    /// Ensures command execution is enabled in workspace-agent options.
    /// </summary>
    /// <exception cref="WorkspaceExecutionException">Thrown when execution is disabled.</exception>
    private void EnsureExecutionEnabled()
    {
        if (!_options.ExecutionEnabled)
        {
            throw new UnauthorizedAccessException(
                "Code execution is disabled for this Workspace runtime. Use an isolated container runtime.");
        }
    }

    /// <summary>
    /// Truncates command output to the configured maximum length.
    /// </summary>
    /// <param name="value">The output text.</param>
    /// <returns>The bounded output text.</returns>
    private string LimitOutput(string value)
    {
        var limit = Math.Max(1, _options.MaxCommandOutputBytes);
        return Encoding.UTF8.GetByteCount(value) <= limit
            ? value
            : value[..Math.Min(value.Length, limit)] + Environment.NewLine + "[output truncated]";
    }

    /// <summary>
    /// Counts non-overlapping occurrences of a value in text.
    /// </summary>
    /// <param name="text">The text to inspect.</param>
    /// <param name="value">The value to count.</param>
    /// <returns>The occurrence count.</returns>
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

    /// <summary>
    /// Detects likely binary content by inspecting a bounded prefix for null bytes or invalid UTF-8.
    /// </summary>
    /// <param name="path">The file path to inspect.</param>
    /// <param name="cancellationToken">Token used to cancel reading.</param>
    /// <returns>A task whose result indicates whether the file appears binary.</returns>
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

    private readonly record struct WorkspaceTextLine(byte[] Bytes, bool Truncated);

    private sealed class WorkspaceTextScanner
    {
        private readonly Stream _stream;
        private readonly byte[] _buffer = new byte[81920];
        private int _count;
        private int _offset;

        public WorkspaceTextScanner(Stream stream)
        {
            _stream = stream;
        }

        /// <summary>
        /// Skips up to the requested number of newline-delimited text lines.
        /// </summary>
        /// <param name="lines">The number of lines to skip.</param>
        /// <param name="cancellationToken">Token used to cancel scanning.</param>
        /// <returns>A task containing the number of lines actually skipped.</returns>
        public async Task<int> SkipLinesAsync(int lines, CancellationToken cancellationToken)
        {
            var skipped = 0;
            while (skipped < lines)
            {
                if (!await EnsureAsync(cancellationToken).ConfigureAwait(false))
                {
                    return skipped;
                }

                var value = _buffer[_offset++];
                if (value == 0)
                {
                    throw new ArgumentException("Workspace text window cannot include binary content.");
                }

                if (value == (byte)'\n')
                {
                    skipped++;
                }
                else if (value == (byte)'\r')
                {
                    skipped++;
                    if (await PeekAsync(cancellationToken).ConfigureAwait(false) == '\n')
                    {
                        _offset++;
                    }
                }
            }

            return skipped;
        }

        /// <summary>
        /// Reads one bounded UTF-8 line, reporting truncation when the line exceeds the byte limit.
        /// </summary>
        /// <param name="maxLineBytes">The maximum line byte count.</param>
        /// <param name="cancellationToken">Token used to cancel scanning.</param>
        /// <returns>A task containing the line, or <see langword="null"/> at end of stream.</returns>
        public async Task<WorkspaceTextLine?> ReadLineAsync(int maxLineBytes, CancellationToken cancellationToken)
        {
            var line = new MemoryStream();
            var any = false;
            while (true)
            {
                if (!await EnsureAsync(cancellationToken).ConfigureAwait(false))
                {
                    return any ? new WorkspaceTextLine(line.ToArray(), false) : null;
                }

                var value = _buffer[_offset++];
                if (value == 0)
                {
                    throw new ArgumentException("Workspace text window cannot include binary content.");
                }

                if (value == (byte)'\n')
                {
                    return new WorkspaceTextLine(line.ToArray(), false);
                }

                if (value == (byte)'\r')
                {
                    if (await PeekAsync(cancellationToken).ConfigureAwait(false) == '\n')
                    {
                        _offset++;
                    }

                    return new WorkspaceTextLine(line.ToArray(), false);
                }

                if (line.Length >= maxLineBytes)
                {
                    await DrainRestOfLineAsync(cancellationToken).ConfigureAwait(false);
                    return new WorkspaceTextLine(line.ToArray(), true);
                }

                any = true;
                line.WriteByte(value);
            }
        }

        /// <summary>
        /// Determines whether unread bytes remain in the scanner stream.
        /// </summary>
        /// <param name="cancellationToken">Token used to cancel probing.</param>
        /// <returns>A task whose result indicates whether more bytes are available.</returns>
        public Task<bool> HasMoreAsync(CancellationToken cancellationToken)
        {
            return EnsureAsync(cancellationToken);
        }

        /// <summary>
        /// Consumes bytes until the current line terminator or end of stream.
        /// </summary>
        /// <param name="cancellationToken">Token used to cancel draining.</param>
        private async Task DrainRestOfLineAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                if (!await EnsureAsync(cancellationToken).ConfigureAwait(false))
                {
                    return;
                }

                var value = _buffer[_offset++];
                if (value == 0)
                {
                    throw new ArgumentException("Workspace text window cannot include binary content.");
                }

                if (value == (byte)'\n')
                {
                    return;
                }

                if (value == (byte)'\r')
                {
                    if (await PeekAsync(cancellationToken).ConfigureAwait(false) == '\n')
                    {
                        _offset++;
                    }

                    return;
                }
            }
        }

        /// <summary>
        /// Reads the next buffered byte without advancing the scanner position.
        /// </summary>
        /// <param name="cancellationToken">Token used to cancel buffering.</param>
        /// <returns>A task containing the next byte, or <c>-1</c> at end of stream.</returns>
        private async Task<int> PeekAsync(CancellationToken cancellationToken)
        {
            if (!await EnsureAsync(cancellationToken).ConfigureAwait(false))
            {
                return -1;
            }

            return _buffer[_offset];
        }

        /// <summary>
        /// Ensures that the scanner buffer contains at least one byte when possible.
        /// </summary>
        /// <param name="cancellationToken">Token used to cancel buffering.</param>
        /// <returns>A task whose result indicates whether a byte is available.</returns>
        private async Task<bool> EnsureAsync(CancellationToken cancellationToken)
        {
            if (_offset < _count)
            {
                return true;
            }

            _count = await _stream.ReadAsync(_buffer, cancellationToken).ConfigureAwait(false);
            _offset = 0;
            return _count > 0;
        }
    }
}
