using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Senparc.Ncf.IsolatedRuntime;
using Senparc.Ncf.Shared.Abstractions.Isolation;
using Senparc.Xncf.Workspace.Abstractions;
using Senparc.Xncf.WorkspaceAgent;

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
    public async Task WriteFile_RejectsEmptyRelativePath()
    {
        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            _engine.WriteFileAsync(new WorkspaceWriteFileRequest(
                string.Empty,
                Encoding.UTF8.GetBytes("content"))));

        Assert.AreEqual(
            "Workspace root cannot be modified as a file entry.",
            exception.Message);
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
    public async Task SuggestPaths_ReturnsFilesAndDirectoriesByRelativePath()
    {
        await _engine.CreateDirectoryAsync("docs");
        await _engine.WriteFileAsync(new WorkspaceWriteFileRequest(
            "src/Admin/Chat.cs", Encoding.UTF8.GetBytes("class Chat {}")));
        await _engine.WriteFileAsync(new WorkspaceWriteFileRequest(
            "src/readme.txt", Encoding.UTF8.GetBytes("workspace readme")));
        await _engine.WriteFileAsync(new WorkspaceWriteFileRequest(
            "notes.txt", Encoding.UTF8.GetBytes("plain note")));
        await _engine.WriteFileAsync(new WorkspaceWriteFileRequest(
            "node_modules/leftpad/index.js", Encoding.UTF8.GetBytes("module.exports = 1;")));

        var root = await _engine.SearchAsync(new WorkspaceSearchRequest(SuggestPaths: true, MaxResults: 20));
        CollectionAssert.AreEquivalent(
            new[] { "docs", "src", "notes.txt" },
            root.Select(item => item.RelativePath).ToArray());
        Assert.IsTrue(root.First().MatchType == "directory");

        var byName = await _engine.SearchAsync(new WorkspaceSearchRequest(
            NameQuery: "chat", SuggestPaths: true, MaxResults: 20));
        Assert.AreEqual("src/Admin/Chat.cs", byName.Single().RelativePath);
        Assert.AreEqual("path", byName.Single().MatchType);

        var byDirectory = await _engine.SearchAsync(new WorkspaceSearchRequest(
            NameQuery: "src/", SuggestPaths: true, MaxResults: 20));
        CollectionAssert.AreEquivalent(
            new[] { "src/Admin", "src/Admin/Chat.cs", "src/readme.txt" },
            byDirectory.Select(item => item.RelativePath).ToArray());
        Assert.IsFalse(byDirectory.Any(item => item.RelativePath.Contains("node_modules", StringComparison.Ordinal)));

        var fileNameSearch = await _engine.SearchAsync(new WorkspaceSearchRequest(NameQuery: "Chat"));
        Assert.HasCount(1, fileNameSearch);
        Assert.IsTrue(fileNameSearch.All(item => item.MatchType == "fileName"));
        await Assert.ThrowsExactlyAsync<ArgumentException>(() =>
            _engine.SearchAsync(new WorkspaceSearchRequest(NameQuery: "../secret", SuggestPaths: true)));
    }

    [TestMethod]
    public async Task SearchSupportsRegexContentAndRelativePathGlob()
    {
        await _engine.WriteFileAsync(new WorkspaceWriteFileRequest(
            "tools/NcfSimulatedSite/Senparc.Areas.Admin/WorkspacePreview.cs",
            Encoding.UTF8.GetBytes(
                "WorkspaceAssets are loaded here.\nContent-Security-Policy is set here.\n")));
        await _engine.WriteFileAsync(new WorkspaceWriteFileRequest(
            "tools/NcfSimulatedSite/Senparc.Areas.Admin/Other.cs",
            Encoding.UTF8.GetBytes("WorkspaceAssets are not a path match.\n")));

        var contentMatches = await _engine.SearchAsync(new WorkspaceSearchRequest(
            ContentQuery: "WorkspaceAssets|Content-Security-Policy|Workspace.*Preview",
            ContentRegex: true,
            MaxResults: 20));
        Assert.HasCount(3, contentMatches);
        Assert.IsTrue(contentMatches.All(item => item.MatchType == "content"));

        var pathMatches = await _engine.SearchAsync(new WorkspaceSearchRequest(
            PathGlob: "tools/NcfSimulatedSite/Senparc.Areas.Admin/**/*Workspace*",
            MaxResults: 20));
        Assert.AreEqual("tools/NcfSimulatedSite/Senparc.Areas.Admin/WorkspacePreview.cs",
            pathMatches.Single().RelativePath);
        Assert.AreEqual("path", pathMatches.Single().MatchType);
    }

    [TestMethod]
    public async Task SearchRejectsInvalidRegexAndPathGlobTraversal()
    {
        await Assert.ThrowsExactlyAsync<ArgumentException>(() =>
            _engine.SearchAsync(new WorkspaceSearchRequest(
                ContentQuery: "(",
                ContentRegex: true)));
        await Assert.ThrowsExactlyAsync<ArgumentException>(() =>
            _engine.SearchAsync(new WorkspaceSearchRequest(
                PathGlob: "../**/*")));
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
        foreach (var extension in new[] { ".dll", ".deps.json", ".runtimeconfig.json" })
        {
            Assert.IsTrue(File.Exists(Path.Combine(AppContext.BaseDirectory, "Senparc.Xncf.WorkspaceAgent" + extension)));
        }

        var options = Options.Create(new WorkspaceOptions
        {
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

    [TestMethod]
    public async Task TextWindow_ReadsLineSliceWithoutLoadingTheWholeFile()
    {
        var lines = Enumerable.Range(1, 8).Select(index => "line-" + index);
        await File.WriteAllTextAsync(Path.Combine(_root, "notes.jsonl"), string.Join("\r\n", lines) + "\r\n");

        var window = await _engine.ReadTextWindowAsync(new WorkspaceTextWindowRequest("notes.jsonl", 3, 2, 4096));
        Assert.AreEqual("line-3\nline-4", window.Content);
        Assert.AreEqual(3, window.StartLine);
        Assert.AreEqual(2, window.LineCount);
        Assert.IsTrue(window.HasMore);
        Assert.IsFalse(window.Truncated);

        var pastEnd = await _engine.ReadTextWindowAsync(new WorkspaceTextWindowRequest("notes.jsonl", 100, 10, 4096));
        Assert.AreEqual(string.Empty, pastEnd.Content);
        Assert.IsFalse(pastEnd.HasMore);

        var oversized = new string('x', 80);
        await File.WriteAllTextAsync(
            Path.Combine(_root, "large.log"),
            string.Join('\n', Enumerable.Repeat(oversized, 2000)) + "\n");
        await Assert.ThrowsExactlyAsync<WorkspaceLimitException>(() => _engine.ReadFileAsync("large.log", 1024));
        var head = await _engine.ReadTextWindowAsync(new WorkspaceTextWindowRequest("large.log", 1, 2, 4096));
        Assert.AreEqual(oversized + "\n" + oversized, head.Content);
        Assert.IsTrue(head.HasMore);
        Assert.IsTrue(head.FileLength > 128 * 1024);

        await File.WriteAllTextAsync(Path.Combine(_root, "wide.txt"), new string('y', 3000) + "\nnext\n");
        var cut = await _engine.ReadTextWindowAsync(new WorkspaceTextWindowRequest("wide.txt", 1, 10, 1024));
        Assert.IsTrue(cut.Truncated);
        Assert.IsTrue(cut.Content.EndsWith('…'));
        Assert.IsTrue(cut.HasMore);
        Assert.IsTrue(cut.Content.Length < 1200);

        await File.WriteAllTextAsync(Path.Combine(_root, "binary.txt"), "a\0b");
        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => _engine.ReadTextWindowAsync(new WorkspaceTextWindowRequest("binary.txt", 1, 10, 4096)));
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => _engine.ReadTextWindowAsync(new WorkspaceTextWindowRequest("../secret.txt", 1, 10, 4096)));
    }
}
