/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：DockerIsolatedRuntimeProvider.cs
    文件功能描述：共享 Docker 隔离运行时实现

    创建标识：Senparc - 20261024
----------------------------------------------------------------*/

using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Senparc.Ncf.Shared.Abstractions.Isolation;

namespace Senparc.Ncf.IsolatedRuntime;

public sealed class DockerIsolatedRuntimeProvider : IIsolatedRuntimeProvider
{
    private readonly DockerIsolatedRuntimeOptions _options;
    private readonly ILogger<DockerIsolatedRuntimeProvider> _logger;

    public DockerIsolatedRuntimeProvider(
        IOptions<DockerIsolatedRuntimeOptions> options,
        ILogger<DockerIsolatedRuntimeProvider> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public string ProviderName => "Docker";

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await RunDockerAsync(
                ["version", "--format", "{{.Server.Version}}"],
                TimeSpan.FromSeconds(Math.Clamp(_options.ProbeTimeoutSeconds, 2, 30)),
                cancellationToken).ConfigureAwait(false);
            return result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.StandardOutput);
        }
        catch (Exception exception) when (exception is
                   IOException or InvalidOperationException or TimeoutException)
        {
            _logger.LogDebug(exception, "Docker isolated runtime probe failed.");
            return false;
        }
    }

    public Task<IsolatedRuntimeCommandResult> RunAsync(
        IsolatedRuntimeSpec specification,
        CancellationToken cancellationToken = default)
    {
        var arguments = DockerCommandBuilder.BuildRunArguments(specification);
        return RunDockerAsync(
            arguments,
            NormalizeTimeout(specification.Timeout),
            cancellationToken,
            specification.MaxOutputCharacters,
            specification.StandardInput);
    }

    public IIsolatedRuntimeConnection StartAttached(IsolatedRuntimeSpec specification)
    {
        if (specification.Detached)
        {
            throw new ArgumentException("Attached runtimes cannot be detached.", nameof(specification));
        }

        var startInfo = CreateStartInfo(DockerCommandBuilder.BuildRunArguments(specification));
        startInfo.RedirectStandardInput = true;
        return ProcessIsolatedRuntimeConnection.Start(startInfo);
    }

    public Task<IsolatedRuntimeCommandResult> ExecAsync(
        IsolatedRuntimeExecRequest request,
        CancellationToken cancellationToken = default)
    {
        return RunDockerAsync(
            DockerCommandBuilder.BuildExecArguments(request),
            NormalizeTimeout(request.Timeout ?? TimeSpan.FromSeconds(30)),
            cancellationToken,
            request.MaxOutputCharacters,
            request.StandardInput);
    }

    public async Task<string> GetLogsAsync(
        string runtimeHandle,
        int tailLines,
        CancellationToken cancellationToken = default)
    {
        EnsureHandle(runtimeHandle);
        var result = await RunDockerAsync(
            ["logs", "--tail", Math.Clamp(tailLines, 1, 10_000).ToString(), runtimeHandle],
            TimeSpan.FromSeconds(30),
            cancellationToken).ConfigureAwait(false);
        return string.Join(Environment.NewLine, result.StandardError, result.StandardOutput).Trim();
    }

    public async Task RemoveAsync(
        string runtimeHandle,
        bool force = true,
        CancellationToken cancellationToken = default)
    {
        EnsureHandle(runtimeHandle);
        var arguments = force
            ? new[] { "rm", "-f", runtimeHandle }
            : new[] { "rm", runtimeHandle };
        var result = await RunDockerAsync(
            arguments,
            TimeSpan.FromSeconds(30),
            cancellationToken).ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"Docker runtime removal failed: {result.StandardError}");
        }
    }

    public async Task<IReadOnlyList<string>> ListHandlesAsync(
        IsolatedRuntimeListRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var arguments = new List<string> { "ps" };
        if (request.IncludeStopped)
        {
            arguments.Add("-a");
        }

        arguments.AddRange(["-q", "--no-trunc"]);
        foreach (var label in request.Labels.OrderBy(item => item.Key, StringComparer.Ordinal))
        {
            arguments.Add("--filter");
            arguments.Add($"label={label.Key}={label.Value}");
        }

        var result = await RunDockerAsync(
            arguments,
            TimeSpan.FromSeconds(30),
            cancellationToken).ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"Docker runtime query failed: {result.StandardError}");
        }

        return result.StandardOutput
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private async Task<IsolatedRuntimeCommandResult> RunDockerAsync(
        IReadOnlyList<string> arguments,
        TimeSpan timeout,
        CancellationToken cancellationToken,
        int maxOutputCharacters = 0,
        string? standardInput = null)
    {
        var startInfo = CreateStartInfo(arguments);
        startInfo.RedirectStandardInput = standardInput != null;
        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        var standardOutput = new StringBuilder();
        var standardError = new StringBuilder();
        process.OutputDataReceived += (_, eventArgs) =>
        {
            if (eventArgs.Data != null)
            {
                AppendLimited(standardOutput, eventArgs.Data, maxOutputCharacters);
            }
        };
        process.ErrorDataReceived += (_, eventArgs) =>
        {
            if (eventArgs.Data != null)
            {
                AppendLimited(standardError, eventArgs.Data, maxOutputCharacters);
            }
        };

        if (!process.Start())
        {
            throw new InvalidOperationException("Docker isolated runtime process could not be started.");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);
        if (standardInput != null)
        {
            try
            {
                await process.StandardInput
                    .WriteAsync(standardInput.AsMemory(), timeoutSource.Token)
                    .ConfigureAwait(false);
                await process.StandardInput
                    .FlushAsync(timeoutSource.Token)
                    .ConfigureAwait(false);
                process.StandardInput.Close();
            }
            catch (OperationCanceledException)
            {
                Terminate(process);
                if (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }

                throw new TimeoutException(
                    $"Docker isolated runtime operation timed out after {timeout.TotalSeconds:0} seconds.");
            }
            catch (Exception exception) when (exception is IOException or InvalidOperationException)
            {
                _logger.LogDebug(exception, "Writing isolated runtime stdin failed.");
            }
        }

        try
        {
            await process.WaitForExitAsync(timeoutSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            Terminate(process);
            if (cancellationToken.IsCancellationRequested)
            {
                throw;
            }

            throw new TimeoutException(
                $"Docker isolated runtime operation timed out after {timeout.TotalSeconds:0} seconds.");
        }

        return new IsolatedRuntimeCommandResult(
            process.ExitCode,
            standardOutput.ToString(),
            standardError.ToString());
    }

    private static void Terminate(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
        }
    }

    private ProcessStartInfo CreateStartInfo(IReadOnlyList<string> arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _options.Executable,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        return startInfo;
    }

    private static void EnsureHandle(string runtimeHandle)
    {
        if (string.IsNullOrWhiteSpace(runtimeHandle) ||
            runtimeHandle.Any(character =>
                !(char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.')))
        {
            throw new ArgumentException("Isolated runtime handle is invalid.", nameof(runtimeHandle));
        }
    }

    private static TimeSpan NormalizeTimeout(TimeSpan timeout) =>
        timeout <= TimeSpan.Zero ? TimeSpan.FromSeconds(30) : timeout;

    private static void AppendLimited(StringBuilder builder, string line, int maximumCharacters)
    {
        if (maximumCharacters <= 0)
        {
            builder.AppendLine(line);
            return;
        }

        if (builder.Length >= maximumCharacters)
        {
            return;
        }

        var remaining = maximumCharacters - builder.Length;
        if (line.Length + Environment.NewLine.Length <= remaining)
        {
            builder.AppendLine(line);
            return;
        }

        builder.Append(line.AsSpan(0, Math.Max(0, remaining - Environment.NewLine.Length)));
        builder.AppendLine();
    }
}
