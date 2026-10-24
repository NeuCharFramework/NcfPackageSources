/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：WorkspaceAgentHost.cs
    文件功能描述：基于 NDJSON/stdin/stdout 的跨平台 Workspace Agent 宿主

    创建标识：Senparc - 20261024
----------------------------------------------------------------*/

using System.Text.Json;
using Senparc.Xncf.Workspace.Abstractions;

namespace Senparc.Xncf.Workspace.Agent;

public static class WorkspaceAgentHost
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task<int> RunAsync(
        string[] args,
        TextReader input,
        TextWriter output,
        TextWriter error,
        CancellationToken cancellationToken)
    {
        WorkspaceAgentOptions options;
        try
        {
            options = ParseOptions(args);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            await error.WriteLineAsync(exception.Message).ConfigureAwait(false);
            return 2;
        }

        WorkspaceFileSystemEngine engine;
        try
        {
            engine = new WorkspaceFileSystemEngine(options);
        }
        catch (Exception exception)
        {
            await error.WriteLineAsync($"Workspace Agent could not start: {exception.Message}").ConfigureAwait(false);
            return 3;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await input.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line == null)
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            WorkspaceAgentRequest? request = null;
            WorkspaceAgentResponse response;
            try
            {
                request = JsonSerializer.Deserialize<WorkspaceAgentRequest>(line, JsonOptions)
                    ?? throw new JsonException("Workspace Agent request is empty.");
                response = await ExecuteAsync(engine, options, request, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                response = CreateErrorResponse(request?.Id ?? string.Empty, exception);
            }

            await output.WriteLineAsync(JsonSerializer.Serialize(response, JsonOptions)).ConfigureAwait(false);
            await output.FlushAsync(cancellationToken).ConfigureAwait(false);
            if (request?.Operation == WorkspaceAgentOperations.Shutdown)
            {
                break;
            }
        }

        return 0;
    }

    private static async Task<WorkspaceAgentResponse> ExecuteAsync(
        WorkspaceFileSystemEngine engine,
        WorkspaceAgentOptions options,
        WorkspaceAgentRequest request,
        CancellationToken cancellationToken)
    {
        object? result = request.Operation switch
        {
            WorkspaceAgentOperations.Ping => new WorkspaceAgentRuntimeInfo(
                Environment.ProcessId,
                Environment.OSVersion.Platform.ToString(),
                engine.RootPath,
                options.ExecutionEnabled,
                options.IsolationLevel),
            WorkspaceAgentOperations.List => await engine.ListAsync(
                Deserialize<WorkspaceAgentListRequest>(request).RelativePath,
                cancellationToken).ConfigureAwait(false),
            WorkspaceAgentOperations.Stat => await engine.GetFileInfoAsync(
                Deserialize<WorkspaceAgentPathRequest>(request).RelativePath,
                cancellationToken).ConfigureAwait(false),
            WorkspaceAgentOperations.Read => await ReadAsync(engine, request, cancellationToken).ConfigureAwait(false),
            WorkspaceAgentOperations.Write => await engine.WriteFileAsync(
                Deserialize<WorkspaceWriteFileRequest>(request),
                cancellationToken).ConfigureAwait(false),
            WorkspaceAgentOperations.CreateDirectory => await CreateDirectoryAsync(
                engine,
                request,
                cancellationToken).ConfigureAwait(false),
            WorkspaceAgentOperations.Delete => await DeleteAsync(
                engine,
                request,
                cancellationToken).ConfigureAwait(false),
            WorkspaceAgentOperations.Move => await MoveAsync(
                engine,
                request,
                cancellationToken).ConfigureAwait(false),
            WorkspaceAgentOperations.Search => await engine.SearchAsync(
                Deserialize<WorkspaceSearchRequest>(request),
                cancellationToken).ConfigureAwait(false),
            WorkspaceAgentOperations.Patch => await engine.ApplyPatchAsync(
                Deserialize<WorkspacePatchRequest>(request),
                cancellationToken).ConfigureAwait(false),
            WorkspaceAgentOperations.ExecuteCommand => await engine.ExecuteCommandAsync(
                Deserialize<WorkspaceCommandRequest>(request),
                cancellationToken).ConfigureAwait(false),
            WorkspaceAgentOperations.ExecutePython => await engine.ExecutePythonAsync(
                Deserialize<WorkspacePythonRequest>(request),
                cancellationToken).ConfigureAwait(false),
            WorkspaceAgentOperations.Shutdown => new { stopped = true },
            _ => throw new ArgumentException("Unsupported Workspace Agent operation.")
        };

        return new WorkspaceAgentResponse(
            request.Id,
            true,
            JsonSerializer.SerializeToElement(result, JsonOptions));
    }

    private static async Task<object?> ReadAsync(
        WorkspaceFileSystemEngine engine,
        WorkspaceAgentRequest request,
        CancellationToken cancellationToken)
    {
        var payload = Deserialize<WorkspaceAgentReadRequest>(request);
        return await engine.ReadFileAsync(
            payload.RelativePath,
            payload.MaxBytes,
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task<object?> CreateDirectoryAsync(
        WorkspaceFileSystemEngine engine,
        WorkspaceAgentRequest request,
        CancellationToken cancellationToken)
    {
        await engine.CreateDirectoryAsync(
            Deserialize<WorkspaceAgentPathRequest>(request).RelativePath,
            cancellationToken).ConfigureAwait(false);
        return null;
    }

    private static async Task<object?> DeleteAsync(
        WorkspaceFileSystemEngine engine,
        WorkspaceAgentRequest request,
        CancellationToken cancellationToken)
    {
        await engine.DeleteEntryAsync(
            Deserialize<WorkspaceDeleteEntryRequest>(request),
            cancellationToken).ConfigureAwait(false);
        return null;
    }

    private static async Task<object?> MoveAsync(
        WorkspaceFileSystemEngine engine,
        WorkspaceAgentRequest request,
        CancellationToken cancellationToken)
    {
        await engine.MoveEntryAsync(
            Deserialize<WorkspaceMoveEntryRequest>(request),
            cancellationToken).ConfigureAwait(false);
        return null;
    }

    private static T Deserialize<T>(WorkspaceAgentRequest request)
    {
        return request.Payload.Deserialize<T>(JsonOptions)
               ?? throw new JsonException($"Workspace Agent payload for {request.Operation} is invalid.");
    }

    private static WorkspaceAgentResponse CreateErrorResponse(string requestId, Exception exception)
    {
        var (code, message) = exception switch
        {
            WorkspaceConflictException => ("conflict", exception.Message),
            UnauthorizedAccessException => ("forbidden", exception.Message),
            FileNotFoundException or DirectoryNotFoundException => ("not-found", exception.Message),
            ArgumentException or JsonException => ("invalid-request", exception.Message),
            IOException => ("io-error", exception.Message),
            OperationCanceledException => ("cancelled", "Workspace operation was cancelled."),
            _ => ("internal-error", "Workspace operation failed.")
        };
        return new WorkspaceAgentResponse(
            requestId,
            false,
            Error: new WorkspaceAgentError(code, message));
    }

    private static WorkspaceAgentOptions ParseOptions(string[] args)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];
            if (!argument.StartsWith("--", StringComparison.Ordinal) || index + 1 >= args.Length)
            {
                throw new ArgumentException($"Invalid Workspace Agent argument: {argument}");
            }

            values[argument[2..]] = args[++index];
        }

        if (!values.TryGetValue("root", out var root) || string.IsNullOrWhiteSpace(root))
        {
            throw new ArgumentException("Workspace Agent requires --root.");
        }

        return new WorkspaceAgentOptions(
            root,
            GetLong(values, "max-workspace-bytes", 2L * 1024 * 1024 * 1024),
            GetInt(values, "max-entries", 5000),
            GetInt(values, "max-read-bytes", 2 * 1024 * 1024),
            GetInt(values, "max-write-bytes", 10 * 1024 * 1024),
            GetInt(values, "max-search-file-bytes", 4 * 1024 * 1024),
            GetInt(values, "max-search-files", 10_000),
            GetInt(values, "max-search-results", 200),
            GetInt(values, "command-timeout-seconds", 60),
            GetInt(values, "max-command-output-bytes", 2 * 1024 * 1024),
            GetBool(values, "execution-enabled", false),
            values.GetValueOrDefault("isolation-level", "NativeFileAccess"),
            values.GetValueOrDefault("allowed-commands", string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    private static int GetInt(IReadOnlyDictionary<string, string> values, string key, int fallback)
    {
        return values.TryGetValue(key, out var value) && int.TryParse(value, out var parsed)
            ? parsed
            : fallback;
    }

    private static long GetLong(IReadOnlyDictionary<string, string> values, string key, long fallback)
    {
        return values.TryGetValue(key, out var value) && long.TryParse(value, out var parsed)
            ? parsed
            : fallback;
    }

    private static bool GetBool(IReadOnlyDictionary<string, string> values, string key, bool fallback)
    {
        return values.TryGetValue(key, out var value) && bool.TryParse(value, out var parsed)
            ? parsed
            : fallback;
    }
}
