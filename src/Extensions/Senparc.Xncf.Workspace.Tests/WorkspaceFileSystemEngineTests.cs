using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Senparc.Ncf.IsolatedRuntime;
using Senparc.Ncf.Shared.Abstractions.Isolation;
using Senparc.Xncf.Workspace.Abstractions;
using Senparc.Xncf.Workspace.Agent;

namespace Senparc.Xncf.Workspace.Tests;

[TestClass]
public sealed class WorkspaceFileSystemEngineTests
{
    private string _root = null!;
    private WorkspaceFileSystemEngine _engine = null!;

    [TestInitialize]
    public void Initialize()
    {
        _root = Path.Combine(Path.GetTempPath(), $"ncf-workspace-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_root);
        File.WriteAllText(Path.Combine(_root, ".workspace.json"), "{}");
        _engine = new WorkspaceFileSystemEngine(new WorkspaceAgentOptions(
            _root,
            MaxWorkspaceBytes: 1024 * 1024,
            MaxFileReadBytes: 128 * 1024,
            MaxFileWriteBytes: 128 * 1024));
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [TestMethod]
    public async Task BinaryFileLifecycle_UsesAtomicWritesAndEtags()
    {
        await _engine.CreateDirectoryAsync("src");
        var firstContent = new byte[] { 0, 1, 2, 3, 255 };
        var created = await _engine.WriteFileAsync(
            new WorkspaceWriteFileRequest("src/data.bin", firstContent));

        Assert.IsTrue(created.Created);
        Assert.IsFalse(string.IsNullOrWhiteSpace(created.Metadata.ETag));
        CollectionAssert.AreEqual(
            firstContent,
            (await _engine.ReadFileAsync("src/data.bin", 1024)).Content);

        var secondContent = new byte[] { 9, 8, 7 };
        var updated = await _engine.WriteFileAsync(
            new WorkspaceWriteFileRequest(
                "src/data.bin",
                secondContent,
                ExpectedETag: created.Metadata.ETag));
        Assert.IsFalse(updated.Created);
        Assert.AreNotEqual(created.Metadata.ETag, updated.Metadata.ETag);

        await _engine.MoveEntryAsync(
            new WorkspaceMoveEntryRequest(
                "src/data.bin",
                "src/moved.bin",
                ExpectedETag: updated.Metadata.ETag));
        Assert.AreEqual(3, (await _engine.GetFileInfoAsync("src/moved.bin")).Length);

        await _engine.DeleteEntryAsync(
            new WorkspaceDeleteEntryRequest("src/moved.bin"));
        await Assert.ThrowsExactlyAsync<FileNotFoundException>(
            () => _engine.GetFileInfoAsync("src/moved.bin"));
    }

    [TestMethod]
    public async Task StaleEtag_IsRejectedWithoutOverwritingFile()
    {
        var first = await _engine.WriteFileAsync(
            new WorkspaceWriteFileRequest("code.cs", Encoding.UTF8.GetBytes("first")));
        await _engine.WriteFileAsync(
            new WorkspaceWriteFileRequest(
                "code.cs",
                Encoding.UTF8.GetBytes("second"),
                ExpectedETag: first.Metadata.ETag));

        await Assert.ThrowsExactlyAsync<WorkspaceConflictException>(() =>
            _engine.WriteFileAsync(
                new WorkspaceWriteFileRequest(
                    "code.cs",
                    Encoding.UTF8.GetBytes("stale"),
                    ExpectedETag: first.Metadata.ETag)));
        Assert.AreEqual(
            "second",
            Encoding.UTF8.GetString((await _engine.ReadFileAsync("code.cs", 1024)).Content));
    }

    [TestMethod]
    public async Task TraversalAndInternalMetadata_AreRejected()
    {
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => _engine.ReadFileAsync("../secret.txt", 1024));
        await Assert.ThrowsExactlyAsync<UnauthorizedAccessException>(
            () => _engine.ReadFileAsync(".workspace.json", 1024));
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => _engine.WriteFileAsync(
                new WorkspaceWriteFileRequest(
                    "/tmp/outside.txt",
                    Encoding.UTF8.GetBytes("blocked"))));

        var entries = await _engine.ListAsync(string.Empty);
        Assert.IsFalse(entries.Any(entry => entry.RelativePath == ".workspace.json"));
    }

    [TestMethod]
    public async Task SearchAndPatch_OperateInsideAgent()
    {
        await _engine.WriteFileAsync(
            new WorkspaceWriteFileRequest(
                "Program.cs",
                Encoding.UTF8.GetBytes("class Program\n{\n    const string Value = \"old\";\n}\n")));

        var search = await _engine.SearchAsync(new WorkspaceSearchRequest(
            NameQuery: "Program",
            ContentQuery: "\"old\""));
        Assert.HasCount(2, search);
        Assert.IsTrue(search.Any(item => item.MatchType == "content" && item.LineNumber == 3));

        var content = await _engine.ReadFileAsync("Program.cs", 4096);
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(content.Content));
        var patch = await _engine.ApplyPatchAsync(
            new WorkspacePatchRequest("Program.cs", "\"old\"", "\"new\"", ExpectedSha256: hash));
        Assert.IsTrue(patch.Applied);
        StringAssert.Contains(
            Encoding.UTF8.GetString((await _engine.ReadFileAsync("Program.cs", 4096)).Content),
            "\"new\"");
    }

    [TestMethod]
    public async Task NativeExecution_IsDisabledByDefault()
    {
        await Assert.ThrowsExactlyAsync<UnauthorizedAccessException>(() =>
            _engine.ExecuteCommandAsync(new WorkspaceCommandRequest(
                "dotnet",
                ["--info"])));
    }

    [TestMethod]
    public async Task NdjsonProtocol_ReturnsStructuredResponseAndShutdown()
    {
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var ping = new WorkspaceAgentRequest(
            "ping-1",
            WorkspaceAgentOperations.Ping,
            JsonSerializer.SerializeToElement(new { }, jsonOptions));
        var shutdown = new WorkspaceAgentRequest(
            "stop-1",
            WorkspaceAgentOperations.Shutdown,
            JsonSerializer.SerializeToElement(new { }, jsonOptions));
        using var input = new StringReader(
            JsonSerializer.Serialize(ping, jsonOptions) + Environment.NewLine +
            JsonSerializer.Serialize(shutdown, jsonOptions) + Environment.NewLine);
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await WorkspaceAgentHost.RunAsync(
            ["--root", _root],
            input,
            output,
            error,
            CancellationToken.None);

        Assert.AreEqual(0, exitCode);
        var responses = output.ToString()
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => JsonSerializer.Deserialize<WorkspaceAgentResponse>(line, jsonOptions))
            .ToArray();
        Assert.HasCount(2, responses);
        Assert.IsTrue(responses[0]!.Succeeded);
        Assert.AreEqual("ping-1", responses[0]!.Id);
        Assert.IsTrue(responses[1]!.Succeeded);
        Assert.AreEqual(string.Empty, error.ToString());
    }

    [TestMethod]
    public async Task NativeAgentProcessPool_RoundTripsOverPersistentStdio()
    {
        var options = Options.Create(new WorkspaceOptions
        {
            AgentPath = Path.Combine(AppContext.BaseDirectory, "Senparc.Xncf.Workspace.Agent.dll"),
            RuntimeProvider = "NativeAgent",
            AllowNativeExecution = false,
            MaxWorkspaceBytes = 1024 * 1024,
            MaxFileReadBytes = 128 * 1024,
            MaxFileWriteBytes = 128 * 1024
        });
        var processFactory = new WorkspaceAgentProcessFactory(
            options,
            NullLogger<WorkspaceAgentProcessFactory>.Instance);
        await using var pool = new WorkspaceAgentConnectionPool(
            processFactory,
            NullLogger<WorkspaceAgentConnectionPool>.Instance);

        var written = await pool.InvokeAsync<WorkspaceWriteFileRequest, WorkspaceWriteFileResult>(
            "ws_01234567890123456789012345678901",
            _root,
            WorkspaceAgentOperations.Write,
            new WorkspaceWriteFileRequest("pool.txt", Encoding.UTF8.GetBytes("through-agent")));
        var read = await pool.InvokeAsync<WorkspaceAgentReadRequest, WorkspaceFileContent>(
            "ws_01234567890123456789012345678901",
            _root,
            WorkspaceAgentOperations.Read,
            new WorkspaceAgentReadRequest("pool.txt", 1024));

        Assert.IsTrue(written.Created);
        Assert.AreEqual("through-agent", Encoding.UTF8.GetString(read.Content));
    }

    [TestMethod]
    [TestCategory("Docker")]
    public async Task DockerAgentProcessPool_UsesSharedAttachedProvider()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable("NCF_RUN_DOCKER_TESTS"),
                "1",
                StringComparison.Ordinal))
        {
            Assert.Inconclusive("Set NCF_RUN_DOCKER_TESTS=1 to run Docker integration.");
        }

        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(
                _root,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute |
                UnixFileMode.OtherRead | UnixFileMode.OtherWrite | UnixFileMode.OtherExecute);
        }

        var provider = new DockerIsolatedRuntimeProvider(
            Options.Create(new DockerIsolatedRuntimeOptions()),
            NullLogger<DockerIsolatedRuntimeProvider>.Instance);
        var options = Options.Create(new WorkspaceOptions
        {
            RuntimeProvider = "Docker",
            DockerImage = Environment.GetEnvironmentVariable("NCF_WORKSPACE_AGENT_IMAGE")
                          ?? "ncf-workspace-agent:validation",
            MaxWorkspaceBytes = 1024 * 1024,
            MaxFileReadBytes = 128 * 1024,
            MaxFileWriteBytes = 128 * 1024
        });
        var processFactory = new WorkspaceAgentProcessFactory(
            options,
            NullLogger<WorkspaceAgentProcessFactory>.Instance,
            [provider]);
        await using var pool = new WorkspaceAgentConnectionPool(
            processFactory,
            NullLogger<WorkspaceAgentConnectionPool>.Instance);

        var written = await pool.InvokeAsync<WorkspaceWriteFileRequest, WorkspaceWriteFileResult>(
            "ws_11234567890123456789012345678901",
            _root,
            WorkspaceAgentOperations.Write,
            new WorkspaceWriteFileRequest("docker.txt", Encoding.UTF8.GetBytes("shared-provider")));
        var read = await pool.InvokeAsync<WorkspaceAgentReadRequest, WorkspaceFileContent>(
            "ws_11234567890123456789012345678901",
            _root,
            WorkspaceAgentOperations.Read,
            new WorkspaceAgentReadRequest("docker.txt", 1024));

        Assert.IsTrue(written.Created);
        Assert.AreEqual("shared-provider", Encoding.UTF8.GetString(read.Content));
    }
}
