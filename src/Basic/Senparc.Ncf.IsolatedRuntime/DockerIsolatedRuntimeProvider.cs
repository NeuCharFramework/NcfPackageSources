/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: DockerIsolatedRuntimeProvider.cs
    File description: Related implementation.

    Created by: Senparc - 20261024

    Modified by: Senparc - 20261103
    Change description: v0.1.0-preview1 implement connection manager and workspace file system provider

    Modified by: Senparc - 20261119
    Change description: v0.1.1-preview2 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using System.ComponentModel;
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

    /// <summary>
    /// Initializes the Docker isolated-runtime provider.
    /// </summary>
    /// <param name="options">The Docker runtime options.</param>
    /// <param name="logger">The provider logger.</param>
    public DockerIsolatedRuntimeProvider(
        IOptions<DockerIsolatedRuntimeOptions> options,
        ILogger<DockerIsolatedRuntimeProvider> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public string ProviderName => "Docker";

    /// <summary>
    /// Probes the Docker daemon and executable within the configured timeout.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel probing.</param>
    /// <returns>A task whose result indicates whether Docker is available.</returns>
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
                   Win32Exception or UnauthorizedAccessException or IOException or
                   InvalidOperationException or TimeoutException)
        {
            _logger.LogDebug(exception, "Docker isolated runtime probe failed.");
            return false;
        }
    }

    /// <summary>
    /// Runs a container from the supplied isolation specification with bounded output and timeout.
    /// </summary>
    /// <param name="specification">The image, arguments, limits, environment, and input specification.</param>
    /// <param name="cancellationToken">Token used to cancel execution.</param>
    /// <returns>A task containing exit code and captured output.</returns>
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

    /// <summary>
    /// Starts an attached container process with redirected standard input/output.
    /// </summary>
    /// <param name="specification">The non-detached isolation specification.</param>
    /// <returns>An interactive runtime connection.</returns>
    /// <exception cref="ArgumentException">Thrown when the specification requests detached execution.</exception>
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

    /// <summary>
    /// Executes a command inside an existing isolated runtime handle.
    /// </summary>
    /// <param name="request">The runtime handle, command, input, limits, and timeout.</param>
    /// <param name="cancellationToken">Token used to cancel execution.</param>
    /// <returns>A task containing exit code and captured output.</returns>
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

    /// <summary>
    /// Retrieves bounded tail logs for an isolated runtime handle.
    /// </summary>
    /// <param name="runtimeHandle">The validated container handle.</param>
    /// <param name="tailLines">The requested tail line count.</param>
    /// <param name="cancellationToken">Token used to cancel log retrieval.</param>
    /// <returns>A task containing combined stderr/stdout logs.</returns>
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

    /// <summary>
    /// Removes a container by handle, optionally forcing termination.
    /// </summary>
    /// <param name="runtimeHandle">The validated container handle.</param>
    /// <param name="force">Whether stopped/running containers should be force-removed.</param>
    /// <param name="cancellationToken">Token used to cancel removal.</param>
    /// <exception cref="InvalidOperationException">Thrown when Docker removal fails.</exception>
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

    /// <summary>
    /// Lists container handles matching the requested labels and stopped-state policy.
    /// </summary>
    /// <param name="request">The labels and stopped-container option.</param>
    /// <param name="cancellationToken">Token used to cancel enumeration.</param>
    /// <returns>A task containing container handles.</returns>
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

    /// <summary>
    /// Runs the Docker executable, streams bounded output, writes optional stdin,
    /// and converts timeout/cancellation into controlled process termination.
    /// </summary>
    /// <param name="arguments">The Docker command arguments.</param>
    /// <param name="timeout">The operation timeout.</param>
    /// <param name="cancellationToken">Token used to cancel execution.</param>
    /// <param name="maxOutputCharacters">The output cap; zero means uncapped.</param>
    /// <param name="standardInput">Optional standard input.</param>
    /// <returns>A task containing exit code and captured output.</returns>
    /// <exception cref="TimeoutException">Thrown when the operation exceeds <paramref name="timeout"/>.</exception>
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

    /// <summary>Best-effort terminates an isolated process tree.</summary>
    /// <param name="process">The process to terminate.</param>
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

    /// <summary>Creates a non-shell Docker process configuration with argument-list escaping.</summary>
    /// <param name="arguments">The Docker arguments.</param>
    /// <returns>The process-start configuration.</returns>
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

    /// <summary>Validates a container handle before using it in a Docker command.</summary>
    /// <param name="runtimeHandle">The runtime handle.</param>
    /// <exception cref="ArgumentException">Thrown when the handle contains unsafe characters.</exception>
    private static void EnsureHandle(string runtimeHandle)
    {
        if (string.IsNullOrWhiteSpace(runtimeHandle) ||
            runtimeHandle.Any(character =>
                !(char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.')))
        {
            throw new ArgumentException("Isolated runtime handle is invalid.", nameof(runtimeHandle));
        }
    }

    /// <summary>Applies the default timeout when a non-positive timeout is supplied.</summary>
    /// <param name="timeout">The requested timeout.</param>
    /// <returns>The effective timeout.</returns>
    private static TimeSpan NormalizeTimeout(TimeSpan timeout) =>
        timeout <= TimeSpan.Zero ? TimeSpan.FromSeconds(30) : timeout;

    /// <summary>Appends one output line without exceeding the configured character cap.</summary>
    /// <param name="builder">The output builder.</param>
    /// <param name="line">The line to append.</param>
    /// <param name="maximumCharacters">The maximum output length, or zero for unlimited.</param>
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
