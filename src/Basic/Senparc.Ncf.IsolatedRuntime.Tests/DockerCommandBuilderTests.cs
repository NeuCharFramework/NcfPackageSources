using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Senparc.Ncf.Shared.Abstractions.Isolation;

namespace Senparc.Ncf.IsolatedRuntime.Tests;

[TestClass]
public sealed class DockerCommandBuilderTests
{
    [TestMethod]
    public void RunArguments_ApplyStructuredSecurityAndResources()
    {
        var root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "ncf-isolation-test"));
        var arguments = DockerCommandBuilder.BuildRunArguments(new IsolatedRuntimeSpec
        {
            Image = "example/runtime@sha256:abc",
            Name = "ncf-test",
            AutoRemove = true,
            OpenStandardInput = true,
            CpuLimit = 0.5,
            MemoryMegabytes = 256,
            PidsLimit = 64,
            Labels = new Dictionary<string, string> { ["ncf.test"] = "1" },
            Environment = new Dictionary<string, string> { ["SAFE_VALUE"] = "yes" },
            Mounts = [new IsolatedRuntimeMount(root, "/workspace")],
            Ports = [new IsolatedRuntimePortBinding("127.0.0.1", 5500, 8080)],
            TmpfsMounts = [new IsolatedRuntimeTmpfsMount("/tmp", "rw,noexec,size=64m")],
            WorkingDirectory = "/workspace",
            Command = ["dotnet", "app.dll"]
        });

        CollectionAssert.Contains(arguments.ToArray(), "--read-only");
        CollectionAssert.Contains(arguments.ToArray(), "--cap-drop");
        CollectionAssert.Contains(arguments.ToArray(), "no-new-privileges");
        CollectionAssert.Contains(arguments.ToArray(), "type=bind,source=" + root + ",target=/workspace");
        CollectionAssert.Contains(arguments.ToArray(), "127.0.0.1:5500:8080");
        CollectionAssert.Contains(arguments.ToArray(), "SAFE_VALUE=yes");
        var argumentArray = arguments.ToArray();
        Assert.IsLessThan(
            Array.IndexOf(argumentArray, "dotnet"),
            Array.IndexOf(argumentArray, "example/runtime@sha256:abc"));
    }

    [TestMethod]
    public void RunArguments_RejectUnsafeMountNetworkAndPort()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
            DockerCommandBuilder.BuildRunArguments(new IsolatedRuntimeSpec
            {
                Image = "example/runtime",
                Mounts = [new IsolatedRuntimeMount("relative", "/workspace")]
            }));
        Assert.ThrowsExactly<ArgumentException>(() =>
            DockerCommandBuilder.BuildRunArguments(new IsolatedRuntimeSpec
            {
                Image = "example/runtime",
                NetworkMode = "bridge;rm"
            }));
        Assert.ThrowsExactly<ArgumentException>(() =>
            DockerCommandBuilder.BuildRunArguments(new IsolatedRuntimeSpec
            {
                Image = "example/runtime",
                Ports = [new IsolatedRuntimePortBinding("192.168.1.1", 80, 80)]
            }));
    }

    [TestMethod]
    public void ExecArguments_PreserveArgumentBoundariesAndStdin()
    {
        var arguments = DockerCommandBuilder.BuildExecArguments(new IsolatedRuntimeExecRequest(
            "abc123",
            ["/bin/sh", "-lc", "printf '%s' \"$VALUE\""],
            "/workspace",
            "input",
            Environment: new Dictionary<string, string> { ["VALUE"] = "a b" }));

        CollectionAssert.AreEqual(
            new[]
            {
                "exec", "-i",
                "--workdir", "/workspace",
                "--env", "VALUE=a b",
                "abc123",
                "/bin/sh", "-lc", "printf '%s' \"$VALUE\""
            },
            arguments.ToArray());
    }

    [TestMethod]
    public void DependencyInjection_RegistersOneProviderWhenCalledByTwoModules()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        services.AddLogging();
        services.AddDockerIsolatedRuntime(configuration);
        services.AddDockerIsolatedRuntime(configuration);

        using var provider = services.BuildServiceProvider();
        var runtimes = provider.GetServices<IIsolatedRuntimeProvider>().ToArray();

        Assert.HasCount(1, runtimes);
        Assert.AreEqual("Docker", runtimes[0].ProviderName);
    }

    [TestMethod]
    [TestCategory("Docker")]
    public async Task DockerProvider_RunsHardenedOneShotContainer()
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
        Assert.IsTrue(await provider.IsAvailableAsync());

        var result = await provider.RunAsync(new IsolatedRuntimeSpec
        {
            Image = "mcr.microsoft.com/dotnet/runtime:10.0",
            AutoRemove = true,
            NetworkMode = "none",
            ReadOnlyRootFileSystem = true,
            DropAllCapabilities = true,
            NoNewPrivileges = true,
            MemoryMegabytes = 128,
            PidsLimit = 32,
            Timeout = TimeSpan.FromSeconds(30),
            Command = ["dotnet", "--info"]
        });

        Assert.AreEqual(0, result.ExitCode, result.StandardError);
        StringAssert.Contains(result.StandardOutput, ".NET runtimes installed");
    }
}
