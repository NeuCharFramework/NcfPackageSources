using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Xncf.Sandbox.Abstractions;
using Senparc.Xncf.Sandbox.Domain.Services;
using Senparc.Xncf.Sandbox.Domain.Services.Runtime;

namespace Senparc.Xncf.Sandbox.Tests;

[TestClass]
public sealed class SandboxOrchestratorStartupTests
{
    [TestMethod]
    public async Task StartAsync_DoesNotWaitForRuntimeProbe()
    {
        var probe = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        using var orchestrator = new SandboxOrchestrator(
            new ThrowingScopeFactory(),
            [new DelayedRuntime(probe.Task)],
            NullLogger<SandboxOrchestrator>.Instance);

        var startTask = orchestrator.StartAsync(CancellationToken.None);

        Assert.IsTrue(startTask.IsCompleted);

        probe.SetResult(false);
        await orchestrator.StopAsync(CancellationToken.None);
    }

    private sealed class ThrowingScopeFactory : IServiceScopeFactory
    {
        public IServiceScope CreateScope() =>
            throw new InvalidOperationException("The scope should not be used while probing.");
    }

    private sealed class DelayedRuntime(Task<bool> availability) : ISandboxRuntime
    {
        public SandboxRuntimeKind Kind => SandboxRuntimeKind.Docker;

        public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default) =>
            availability;

        public Task<SandboxCreateRuntimeResult> CreateInteractiveAsync(
            SandboxCreateRuntimeRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<SandboxExecResult> ExecAsync(
            SandboxExecRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<SandboxExecResult> ExecInteractiveAsync(
            SandboxInteractiveExecRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task DestroyAsync(string runtimeHandle, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<string>> ListOrphanHandlesAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<string>>([]);

        public Task<IReadOnlyList<string>> ListRunningHandlesAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<string>>([]);
    }
}
