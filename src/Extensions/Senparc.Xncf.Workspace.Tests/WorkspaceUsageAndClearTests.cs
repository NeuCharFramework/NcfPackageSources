/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：WorkspaceUsageAndClearTests.cs
    文件功能描述：工作区用量统计、内容清空与管理员隔离测试


    创建标识：Senparc - 20261109

----------------------------------------------------------------*/

using System.Security.Claims;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Senparc.Xncf.Workspace.Abstractions;
using Senparc.Xncf.Workspace.Domain.Services;

namespace Senparc.Xncf.Workspace.Tests;

[TestClass]
public sealed class WorkspaceDirectoryMaintenanceTests
{
    private string _directory = null!;

    [TestInitialize]
    public void Initialize()
    {
        _directory = Path.Combine(Path.GetTempPath(), $"ncf-workspace-usage-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_directory);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [TestMethod]
    public void Measure_CountsContentWithoutManifestOrSymlinkTargets()
    {
        var root = Path.Combine(_directory, "ws");
        Directory.CreateDirectory(Path.Combine(root, "input"));
        Directory.CreateDirectory(Path.Combine(root, "output", "nested"));
        File.WriteAllText(Path.Combine(root, ".workspace.json"), "{\"id\":\"ws\"}");
        File.WriteAllBytes(Path.Combine(root, "input", "a.txt"), new byte[4]);
        File.WriteAllBytes(Path.Combine(root, "output", "nested", "b.txt"), new byte[6]);
        var outside = Path.Combine(_directory, "outside.bin");
        File.WriteAllBytes(outside, new byte[100]);
        File.CreateSymbolicLink(Path.Combine(root, "input", "linked.bin"), outside);

        var usage = WorkspaceDirectoryMaintenance.Measure(root, 1000);

        Assert.AreEqual(2, usage.FileCount);
        Assert.AreEqual(3, usage.DirectoryCount);
        Assert.AreEqual(10, usage.UsedBytes);
        Assert.AreEqual(1000, usage.QuotaBytes);
        Assert.AreEqual(100, new FileInfo(outside).Length);
    }

    [TestMethod]
    public void Reset_RestoresInitialDirectoriesAndKeepsManifest()
    {
        var root = Path.Combine(_directory, "ws");
        Directory.CreateDirectory(Path.Combine(root, "input"));
        Directory.CreateDirectory(Path.Combine(root, "extra"));
        Directory.CreateDirectory(Path.Combine(root, ".workspace"));
        File.WriteAllText(Path.Combine(root, ".workspace.json"), "manifest-body");
        File.WriteAllText(Path.Combine(root, "input", "a.txt"), "a");
        File.WriteAllText(Path.Combine(root, "extra", "c.txt"), "c");
        File.WriteAllText(Path.Combine(root, ".workspace", "state.json"), "state");
        var outsideDirectory = Path.Combine(_directory, "outside");
        Directory.CreateDirectory(outsideDirectory);
        File.WriteAllText(Path.Combine(outsideDirectory, "secret.txt"), "secret");
        Directory.CreateSymbolicLink(Path.Combine(root, "linked-dir"), outsideDirectory);

        WorkspaceDirectoryMaintenance.ResetToInitialLayout(root);

        Assert.AreEqual("manifest-body", File.ReadAllText(Path.Combine(root, ".workspace.json")));
        Assert.AreEqual("secret", File.ReadAllText(Path.Combine(outsideDirectory, "secret.txt")));
        Assert.IsFalse(Directory.Exists(Path.Combine(root, "extra")));
        Assert.IsFalse(Directory.Exists(Path.Combine(root, "linked-dir")));
        foreach (var directory in new[] { "input", "output", "logs", ".workspace" })
        {
            var path = Path.Combine(root, directory);
            Assert.IsTrue(Directory.Exists(path), directory);
            Assert.IsEmpty(Directory.EnumerateFileSystemEntries(path));
        }

        var usage = WorkspaceDirectoryMaintenance.Measure(root, 2048);
        Assert.AreEqual(0, usage.FileCount);
        Assert.AreEqual(0, usage.UsedBytes);
        Assert.AreEqual(4, usage.DirectoryCount);
    }
}

[TestClass]
public sealed class WorkspaceCatalogTests
{
    [TestMethod]
    public void ActorResolution_RecognizesAdministratorRoleOnly()
    {
        Assert.IsTrue(WorkspaceActors.HasSuperAdminRole(Principal("7", "administrator")));
        Assert.IsTrue(WorkspaceActors.HasSuperAdminRole(Principal("7", "Administrator, operator")));
        Assert.IsFalse(WorkspaceActors.HasSuperAdminRole(Principal("7", "operator")));
        Assert.IsFalse(WorkspaceActors.HasSuperAdminRole(Principal("7", "Admin")));

        Assert.IsTrue(WorkspaceActors.TryResolve(Principal("7", "operator"), out var actor));
        Assert.AreEqual(7, actor.UserId);
        Assert.IsFalse(actor.IsSuperAdmin);
    }

    [TestMethod]
    public async Task Report_HidesOtherOwnersFromRegularAdministrator()
    {
        var storage = new FakeStorage();
        var catalog = CreateCatalog(storage);

        var report = await catalog.GetReportAsync(new WorkspaceActor(7, false));

        Assert.HasCount(1, report.OwnWorkspaces);
        Assert.IsEmpty(report.OtherWorkspaces);
        Assert.AreEqual(1, report.OwnSummary.WorkspaceCount);
        Assert.AreEqual(4, report.OwnSummary.FileCount);
        Assert.AreEqual(40, report.OwnSummary.UsedBytes);
        Assert.AreEqual(report.OwnSummary, report.VisibleSummary);
        CollectionAssert.AreEqual(new[] { "ws_own" }, storage.MeasuredIds.ToArray());
    }

    [TestMethod]
    public async Task Report_SuperAdminSeesOwnAndOtherTotalsSeparately()
    {
        var storage = new FakeStorage();
        var catalog = CreateCatalog(storage);

        var report = await catalog.GetReportAsync(new WorkspaceActor(7, true));

        Assert.IsTrue(report.IsSuperAdmin);
        Assert.AreEqual("Own", report.OwnWorkspaces[0].Scope.ToString());
        Assert.AreEqual(WorkspaceOwnershipScope.Other, report.OtherWorkspaces[0].Scope);
        Assert.AreEqual(9, report.OtherWorkspaces[0].OwnerId);
        Assert.AreEqual(1, report.OwnSummary.WorkspaceCount);
        Assert.AreEqual(1, report.OtherSummary.WorkspaceCount);
        Assert.AreEqual(2, report.VisibleSummary.WorkspaceCount);
        Assert.AreEqual(9, report.VisibleSummary.FileCount);
        Assert.AreEqual(90, report.VisibleSummary.UsedBytes);
        Assert.AreEqual(50, report.QuotaBytes);
    }

    [TestMethod]
    public async Task Clear_RegularAdministratorCannotTouchAnotherOwnersWorkspace()
    {
        var storage = new FakeStorage();
        var catalog = CreateCatalog(storage);

        await Assert.ThrowsExactlyAsync<DirectoryNotFoundException>(() =>
            catalog.ClearContentAsync(new WorkspaceActor(7, false), "ws_other"));

        Assert.IsEmpty(storage.ClearedIds);
    }

    [TestMethod]
    public async Task Clear_OwnerAndSuperAdminAreAllowed()
    {
        var storage = new FakeStorage();
        var catalog = CreateCatalog(storage);

        var own = await catalog.ClearContentAsync(new WorkspaceActor(7, false), "ws_own");
        var other = await catalog.ClearContentAsync(new WorkspaceActor(7, true), "ws_other");

        Assert.IsFalse(own.ClearedOtherOwner);
        Assert.IsTrue(other.ClearedOtherOwner);
        Assert.AreEqual(9, other.OwnerId);
        CollectionAssert.AreEqual(new[] { "ws_own", "ws_other" }, storage.ClearedIds.ToArray());
    }

    [TestMethod]
    public async Task Clear_MissingWorkspaceDoesNotDeleteAnything()
    {
        var storage = new FakeStorage();
        var catalog = CreateCatalog(storage);

        await Assert.ThrowsExactlyAsync<DirectoryNotFoundException>(() =>
            catalog.ClearContentAsync(new WorkspaceActor(7, true), "ws_missing"));

        Assert.IsEmpty(storage.ClearedIds);
    }

    [TestMethod]
    public async Task Create_UsesTheSignedInAdministratorAsOwner()
    {
        var storage = new FakeStorage();
        var catalog = CreateCatalog(storage);

        var created = await catalog.CreateOwnAsync(new WorkspaceActor(7, false), " 订单整理 ");

        Assert.AreEqual(7, created.OwnerId);
        Assert.AreEqual("订单整理", created.Name);
    }

    private static WorkspaceCatalog CreateCatalog(FakeStorage storage) =>
        new(storage, Options.Create(new WorkspaceOptions { MaxWorkspaceBytes = 50 }), NullLogger<WorkspaceCatalog>.Instance);

    private static ClaimsPrincipal Principal(string userId, string roles) =>
        new(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId), new Claim(ClaimTypes.Role, roles)],
            "test"));

    private sealed class FakeStorage : IWorkspaceStorage
    {
        private readonly WorkspaceDescriptor _own = Descriptor("ws_own", 7, "我的资料");
        private readonly WorkspaceDescriptor _other = Descriptor("ws_other", 9, "别人的资料");

        public List<string> MeasuredIds { get; } = [];

        public List<string> ClearedIds { get; } = [];

        public Task<WorkspaceDescriptor> CreateAsync(
            WorkspaceCreateRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Descriptor("ws_created", request.OwnerId, request.Name));

        public Task<WorkspaceDescriptor?> GetAsync(
            string workspaceId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Find(workspaceId));

        public Task<IReadOnlyList<WorkspaceDescriptor>> ListWorkspacesAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<WorkspaceDescriptor>>([_own, _other]);

        public Task<WorkspaceUsageStatistics> GetUsageAsync(
            string workspaceId,
            CancellationToken cancellationToken = default)
        {
            MeasuredIds.Add(workspaceId);
            var descriptor = Find(workspaceId) ?? throw new DirectoryNotFoundException();
            var fileCount = descriptor.OwnerId == 7 ? 4 : 5;
            return Task.FromResult(new WorkspaceUsageStatistics(fileCount, 2, fileCount * 10, 50));
        }

        public Task ClearContentAsync(
            string workspaceId,
            CancellationToken cancellationToken = default)
        {
            ClearedIds.Add(workspaceId);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<WorkspaceEntry>> ListAsync(string workspaceId, string relativePath = "", CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<string> ResolveFilePathAsync(string workspaceId, string relativePath, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<string> ReadFileAsync(string workspaceId, string relativePath, int maxBytes = 2097152, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<WorkspaceFileMetadata> GetFileInfoAsync(string workspaceId, string relativePath, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<WorkspaceFileContent> ReadFileBytesAsync(string workspaceId, string relativePath, int maxBytes = 2097152, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<WorkspaceTextWindow> ReadTextWindowAsync(string workspaceId, WorkspaceTextWindowRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task WriteFileAsync(string workspaceId, string relativePath, string content, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<WorkspaceWriteFileResult> WriteFileBytesAsync(string workspaceId, WorkspaceWriteFileRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<WorkspaceWriteFileResult> WriteFileStreamAsync(string workspaceId, WorkspaceStreamWriteRequest request, Stream content, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task CreateDirectoryAsync(string workspaceId, string relativePath, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteEntryAsync(string workspaceId, WorkspaceDeleteEntryRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task MoveEntryAsync(string workspaceId, WorkspaceMoveEntryRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<WorkspaceSearchResult>> SearchAsync(string workspaceId, WorkspaceSearchRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<WorkspacePatchResult> ApplyPatchAsync(string workspaceId, WorkspacePatchRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<WorkspaceCommandResult> ExecuteCommandAsync(string workspaceId, WorkspaceCommandRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<WorkspaceCommandResult> ExecutePythonAsync(string workspaceId, WorkspacePythonRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(string workspaceId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        private WorkspaceDescriptor? Find(string workspaceId) => workspaceId switch
        {
            "ws_own" => _own,
            "ws_other" => _other,
            _ => null
        };

        private static WorkspaceDescriptor Descriptor(string id, int ownerId, string name) =>
            new(id, ownerId, name, "/tmp/" + id, DateTime.UnixEpoch, DateTime.UnixEpoch);
    }
}
