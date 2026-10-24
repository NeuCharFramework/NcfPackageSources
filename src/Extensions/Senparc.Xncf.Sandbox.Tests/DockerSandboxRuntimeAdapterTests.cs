using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Ncf.IsolatedRuntime;
using Senparc.Ncf.Shared.Abstractions.Isolation;
using Senparc.Xncf.Sandbox.Domain.Services.Runtime;

namespace Senparc.Xncf.Sandbox.Tests;

[TestClass]
public sealed class DockerSandboxRuntimeAdapterTests
{
    [TestMethod]
    public async Task ExecAsync_UsesSecureSharedRuntimeSpecification()
    {
        var provider = new CapturingRuntimeProvider();
        var runtime = CreateRuntime(provider);
        Assert.IsTrue(SandboxTemplateCatalog.TryGet("python-exec", out var template));

        var result = await runtime.ExecAsync(new SandboxExecRequest
        {
            SessionId = Guid.NewGuid().ToString("N"),
            Template = template,
            Code = "print('ok')",
            CpuLimit = 0.5,
            MemoryMb = 256,
            Timeout = TimeSpan.FromSeconds(10)
        });

        Assert.AreEqual("ok", result.StdOut);
        var specification = provider.LastRunSpecification;
        Assert.IsNotNull(specification);
        Assert.AreEqual("none", specification.NetworkMode);
        Assert.IsTrue(specification.AutoRemove);
        Assert.IsTrue(specification.ReadOnlyRootFileSystem);
        Assert.IsTrue(specification.DropAllCapabilities);
        Assert.IsTrue(specification.NoNewPrivileges);
        Assert.IsTrue(specification.Mounts.Single().ReadOnly);
        CollectionAssert.AreEqual(
            new[] { "python", "/work/main.py" },
            specification.Command.ToArray());
    }

    [TestMethod]
    public async Task InteractiveRuntime_UsesSharedPortsMountsAndLabels()
    {
        var provider = new CapturingRuntimeProvider();
        var runtime = CreateRuntime(provider);
        Assert.IsTrue(SandboxTemplateCatalog.TryGet("jupyter-python", out var template));
        var workspace = Path.Combine(
            Path.GetTempPath(),
            $"ncf-sandbox-adapter-{Guid.NewGuid():N}");
        try
        {
            var result = await runtime.CreateInteractiveAsync(new SandboxCreateRuntimeRequest
            {
                SessionId = "session123",
                Template = template,
                CpuLimit = 1,
                MemoryMb = 512,
                WorkspaceDirectory = workspace,
                ExtraPortMappings =
                [
                    new SandboxPortMapping(5510, 5000, false)
                ]
            });

            Assert.AreEqual("container-id", result.RuntimeHandle);
            var specification = provider.LastRunSpecification;
            Assert.IsNotNull(specification);
            Assert.IsTrue(specification.Detached);
            Assert.IsTrue(specification.DropAllCapabilities);
            Assert.IsTrue(specification.NoNewPrivileges);
            Assert.AreEqual("1", specification.Labels["ncf.sandbox"]);
            Assert.AreEqual("session123", specification.Labels["ncf.sandbox.session"]);
            Assert.IsTrue(specification.Ports.Any(port =>
                port.HostPort == 5510 &&
                port.ContainerPort == 5000 &&
                port.HostAddress == "127.0.0.1"));
            Assert.AreEqual("/home/jovyan/work", specification.Mounts.Single().Target);
        }
        finally
        {
            if (Directory.Exists(workspace))
            {
                Directory.Delete(workspace, recursive: true);
            }
        }
    }

    [TestMethod]
    public async Task LifecycleOperations_DelegateToSharedProvider()
    {
        var provider = new CapturingRuntimeProvider();
        var runtime = CreateRuntime(provider);

        await runtime.DestroyAsync("container-id");
        var all = await runtime.ListOrphanHandlesAsync();
        var running = await runtime.ListRunningHandlesAsync();

        Assert.AreEqual("container-id", provider.RemovedHandle);
        Assert.IsTrue(provider.LastRemoveForced);
        Assert.AreEqual(1, all.Count);
        Assert.AreEqual(1, running.Count);
        Assert.AreEqual(2, provider.ListRequests.Count);
        Assert.IsTrue(provider.ListRequests[0].IncludeStopped);
        Assert.IsFalse(provider.ListRequests[1].IncludeStopped);
    }

    [TestMethod]
    [TestCategory("Docker")]
    public async Task PythonExec_RunsThroughRealSharedProvider()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable("NCF_RUN_DOCKER_TESTS"),
                "1",
                StringComparison.Ordinal))
        {
            Assert.Inconclusive("Set NCF_RUN_DOCKER_TESTS=1 to run Docker integration.");
        }

        var provider = new DockerIsolatedRuntimeProvider(
            Options.Create(new DockerIsolatedRuntimeOptions()),
            NullLogger<DockerIsolatedRuntimeProvider>.Instance);
        var runtime = CreateRuntime(provider);
        Assert.IsTrue(SandboxTemplateCatalog.TryGet("python-exec", out var template));

        var result = await runtime.ExecAsync(new SandboxExecRequest
        {
            SessionId = Guid.NewGuid().ToString("N"),
            Template = template,
            Code = "print('shared-sandbox-provider')",
            CpuLimit = 0.25,
            MemoryMb = 128,
            Timeout = TimeSpan.FromMinutes(2)
        });

        Assert.AreEqual(0, result.ExitCode, result.StdErr);
        StringAssert.Contains(result.StdOut, "shared-sandbox-provider");
    }

    private static DockerSandboxRuntime CreateRuntime(IIsolatedRuntimeProvider provider)
    {
        return new DockerSandboxRuntime(
            NullLogger<DockerSandboxRuntime>.Instance,
            new SandboxImageResolver(new SandboxImageOptions()),
            [provider],
            Options.Create(new SandboxDockerOptions()));
    }

    private sealed class CapturingRuntimeProvider : IIsolatedRuntimeProvider
    {
        public string ProviderName => "Docker";

        public IsolatedRuntimeSpec? LastRunSpecification { get; private set; }

        public string? RemovedHandle { get; private set; }

        public bool LastRemoveForced { get; private set; }

        public List<IsolatedRuntimeListRequest> ListRequests { get; } = [];

        public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<IsolatedRuntimeCommandResult> RunAsync(
            IsolatedRuntimeSpec specification,
            CancellationToken cancellationToken = default)
        {
            LastRunSpecification = specification;
            return Task.FromResult(new IsolatedRuntimeCommandResult(
                0,
                specification.Detached ? "container-id" : "ok",
                string.Empty));
        }

        public IIsolatedRuntimeConnection StartAttached(IsolatedRuntimeSpec specification) =>
            throw new NotSupportedException();

        public Task<IsolatedRuntimeCommandResult> ExecAsync(
            IsolatedRuntimeExecRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new IsolatedRuntimeCommandResult(0, "ok", string.Empty));

        public Task<string> GetLogsAsync(
            string runtimeHandle,
            int tailLines,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(string.Empty);

        public Task RemoveAsync(
            string runtimeHandle,
            bool force = true,
            CancellationToken cancellationToken = default)
        {
            RemovedHandle = runtimeHandle;
            LastRemoveForced = force;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<string>> ListHandlesAsync(
            IsolatedRuntimeListRequest request,
            CancellationToken cancellationToken = default)
        {
            ListRequests.Add(request);
            return Task.FromResult<IReadOnlyList<string>>(["container-id"]);
        }
    }
}
