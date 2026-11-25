/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ProcessIsolatedRuntimeConnection.cs
    File description: Related implementation.

    Created by: Senparc - 20261024

    Modified by: Senparc - 20261103
    Change description: v0.1.0-preview1 implement connection manager and workspace file system provider

    Modified by: Senparc - 20261119
    Change description: v0.1.1-preview2 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using System.Diagnostics;
using Senparc.Ncf.Shared.Abstractions.Isolation;

namespace Senparc.Ncf.IsolatedRuntime;

public sealed class ProcessIsolatedRuntimeConnection : IIsolatedRuntimeConnection
{
    private readonly Process _process;
    private bool _disposed;

    /// <summary>
    /// Wraps a started isolated-runtime process.
    /// </summary>
    /// <param name="process">The process whose streams and lifecycle are exposed.</param>
    private ProcessIsolatedRuntimeConnection(Process process)
    {
        _process = process;
    }

    public int ProcessId => _process.Id;

    public bool HasExited => _process.HasExited;

    public int ExitCode => _process.ExitCode;

    public StreamWriter StandardInput => _process.StandardInput;

    public StreamReader StandardOutput => _process.StandardOutput;

    public StreamReader StandardError => _process.StandardError;

    /// <summary>
    /// Starts an isolated-runtime process from the supplied start information.
    /// </summary>
    /// <param name="startInfo">The executable, arguments, and stream redirection settings.</param>
    /// <returns>A connection wrapper for the started process.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="startInfo"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the process cannot be started.</exception>
    public static ProcessIsolatedRuntimeConnection Start(ProcessStartInfo startInfo)
    {
        ArgumentNullException.ThrowIfNull(startInfo);
        var process = Process.Start(startInfo)
                      ?? throw new InvalidOperationException("Isolated runtime process could not be started.");
        return new ProcessIsolatedRuntimeConnection(process);
    }

    /// <summary>
    /// Asynchronously waits for the wrapped process to exit.
    /// </summary>
    /// <param name="cancellationToken">The token used to stop waiting.</param>
    /// <returns>A task that completes when the process exits or waiting is cancelled.</returns>
    public Task WaitForExitAsync(CancellationToken cancellationToken = default) =>
        _process.WaitForExitAsync(cancellationToken);

    /// <summary>
    /// Terminates the process tree when it is still running.
    /// </summary>
    public void Terminate()
    {
        if (_process.HasExited)
        {
            return;
        }

        try
        {
            _process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
        }
    }

    /// <summary>
    /// Terminates and disposes the wrapped process exactly once.
    /// </summary>
    /// <returns>A value task that completes after process termination and disposal.</returns>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (!_process.HasExited)
        {
            Terminate();
            await _process.WaitForExitAsync().ConfigureAwait(false);
        }

        _process.Dispose();
    }
}
