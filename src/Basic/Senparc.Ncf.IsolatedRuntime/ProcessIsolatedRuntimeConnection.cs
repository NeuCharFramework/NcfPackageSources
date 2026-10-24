/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：ProcessIsolatedRuntimeConnection.cs
    文件功能描述：隔离运行时附加进程连接

    创建标识：Senparc - 20261024
----------------------------------------------------------------*/

using System.Diagnostics;
using Senparc.Ncf.Shared.Abstractions.Isolation;

namespace Senparc.Ncf.IsolatedRuntime;

public sealed class ProcessIsolatedRuntimeConnection : IIsolatedRuntimeConnection
{
    private readonly Process _process;
    private bool _disposed;

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

    public static ProcessIsolatedRuntimeConnection Start(ProcessStartInfo startInfo)
    {
        ArgumentNullException.ThrowIfNull(startInfo);
        var process = Process.Start(startInfo)
                      ?? throw new InvalidOperationException("Isolated runtime process could not be started.");
        return new ProcessIsolatedRuntimeConnection(process);
    }

    public Task WaitForExitAsync(CancellationToken cancellationToken = default) =>
        _process.WaitForExitAsync(cancellationToken);

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
