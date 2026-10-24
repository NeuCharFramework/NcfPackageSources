using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Senparc.Ncf.Shared.Abstractions.Isolation;

namespace Senparc.Xncf.Workspace.Tests;

[TestClass]
public sealed class WorkspaceIsolatedRuntimeAdapterTests
{
    [TestMethod]
    public async Task DockerAgent_UsesSharedSecureAttachedRuntime()
    {
        var provider = new CapturingProvider();
        var factory = new WorkspaceAgentProcessFactory(
            Options.Create(new WorkspaceOptions
            {
                RuntimeProvider = "Docker",
                DockerImage = "senparc/ncf-workspace-agent:test",
                DockerCpuLimit = 0.75,
                DockerMemoryMegabytes = 384
            }),
            NullLogger<WorkspaceAgentProcessFactory>.Instance,
            [provider]);
        var workspaceRoot = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "ncf-workspace-adapter"));

        await using var connection = factory.Start(
            "ws_01234567890123456789012345678901",
            workspaceRoot);

        var specification = provider.LastSpecification;
        Assert.IsNotNull(specification);
        Assert.IsTrue(specification.AutoRemove);
        Assert.IsTrue(specification.OpenStandardInput);
        Assert.AreEqual("none", specification.NetworkMode);
        Assert.IsTrue(specification.ReadOnlyRootFileSystem);
        Assert.IsTrue(specification.DropAllCapabilities);
        Assert.IsTrue(specification.NoNewPrivileges);
        Assert.AreEqual(0.75, specification.CpuLimit);
        Assert.AreEqual(384, specification.MemoryMegabytes);
        Assert.AreEqual("/workspace", specification.Mounts.Single().Target);
        Assert.AreEqual("1", specification.Labels["ncf.workspace"]);
        CollectionAssert.Contains(specification.Command.ToArray(), "--execution-enabled");
        CollectionAssert.Contains(specification.Command.ToArray(), "Container");
    }

    private sealed class CapturingProvider : IIsolatedRuntimeProvider
    {
        public string ProviderName => "Docker";

        public IsolatedRuntimeSpec? LastSpecification { get; private set; }

        public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<IsolatedRuntimeCommandResult> RunAsync(
            IsolatedRuntimeSpec specification,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public IIsolatedRuntimeConnection StartAttached(IsolatedRuntimeSpec specification)
        {
            LastSpecification = specification;
            return new FakeConnection();
        }

        public Task<IsolatedRuntimeCommandResult> ExecAsync(
            IsolatedRuntimeExecRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<string> GetLogsAsync(
            string runtimeHandle,
            int tailLines,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task RemoveAsync(
            string runtimeHandle,
            bool force = true,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<string>> ListHandlesAsync(
            IsolatedRuntimeListRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeConnection : IIsolatedRuntimeConnection
    {
        private readonly MemoryStream _input = new();
        private readonly MemoryStream _output = new();
        private readonly MemoryStream _error = new();

        public int ProcessId => 42;

        public bool HasExited => true;

        public int ExitCode => 0;

        public StreamWriter StandardInput { get; }

        public StreamReader StandardOutput { get; }

        public StreamReader StandardError { get; }

        public FakeConnection()
        {
            StandardInput = new StreamWriter(_input);
            StandardOutput = new StreamReader(_output);
            StandardError = new StreamReader(_error);
        }

        public Task WaitForExitAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public void Terminate()
        {
        }

        public async ValueTask DisposeAsync()
        {
            await StandardInput.DisposeAsync().ConfigureAwait(false);
            StandardOutput.Dispose();
            StandardError.Dispose();
            _input.Dispose();
            _output.Dispose();
            _error.Dispose();
        }
    }
}
