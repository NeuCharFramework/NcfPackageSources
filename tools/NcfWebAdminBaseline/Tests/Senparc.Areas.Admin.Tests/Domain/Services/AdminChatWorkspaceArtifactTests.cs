using System.Linq.Expressions;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Senparc.Areas.Admin.ACL;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel.Dto;
using Senparc.Areas.Admin.Domain.Services;
using Senparc.Xncf.Workspace.Abstractions;

namespace Senparc.Areas.Admin.Tests.Domain.Services;

[TestClass]
public class AdminChatWorkspaceArtifactTests
{
    [TestMethod]
    public async Task CollectWorkspaceArtifacts_ParsesWorkspaceAndToolResultChanges()
    {
        var session = new AdminChatSession("Workspace artifacts", 7, "ws_test");
        typeof(AdminChatSession).GetProperty("Id")!.SetValue(session, 42);

        var repository = new Mock<IAdminChatSessionRepository>();
        repository.Setup(value => value.GetFirstOrDefaultObjectAsync(
                It.IsAny<Expression<Func<AdminChatSession, bool>>>(), It.IsAny<string[]>()))
            .ReturnsAsync((Expression<Func<AdminChatSession, bool>> predicate, string[] _) =>
                predicate.Compile()(session) ? session : null);

        var storage = new Mock<IWorkspaceStorage>();
        storage.Setup(value => value.GetFileInfoAsync(
                "ws_test", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, string path, CancellationToken _) =>
                new WorkspaceFileMetadata(path, false, 12, DateTime.UtcNow, DateTime.UtcNow, "etag-" + path));

        using var services = new ServiceCollection()
            .AddSingleton<IWorkspaceStorage>(storage.Object)
            .AddSingleton<AdminChatSessionService>(provider =>
                new AdminChatSessionService(repository.Object, provider))
            .BuildServiceProvider();
        var messages = new AdminChatMessageService(
            Mock.Of<IAdminChatMessageRepository>(), services);

        var toolResult = JsonSerializer.Serialize(new
        {
            CallId = "call-1",
            Result = JsonSerializer.Serialize(new
            {
                success = true,
                change = new { path = "output/changed.txt", changeType = "modified" }
            })
        });
        var workspaceChange = JsonSerializer.Serialize(new
        {
            CallId = "call-2",
            change = new { path = "output/new.txt", changeType = "added" }
        });
        var deletedChange = JsonSerializer.Serialize(new
        {
            change = new { path = "output/old.txt", changeType = "deleted" }
        });

        var result = await messages.CollectWorkspaceArtifactsAsync(42, new AdminChatHarnessResult
        {
            TrajectoryEvents =
            [
                new AdminChatTrajectoryEventDto { EventType = "tool.result", PayloadJson = toolResult },
                new AdminChatTrajectoryEventDto { EventType = "workspace.change", PayloadJson = workspaceChange },
                new AdminChatTrajectoryEventDto { EventType = "workspace.change", PayloadJson = deletedChange }
            ]
        });

        Assert.AreEqual(3, result.Count);
        CollectionAssert.AreEquivalent(
            new[] { "output/changed.txt", "output/new.txt", "output/old.txt" },
            result.Select(item => item.RelativePath).ToArray());
        Assert.AreEqual("changed", result.Single(item => item.RelativePath == "output/changed.txt").ChangeType);
        Assert.AreEqual("created", result.Single(item => item.RelativePath == "output/new.txt").ChangeType);
        Assert.AreEqual("deleted", result.Single(item => item.RelativePath == "output/old.txt").ChangeType);
        Assert.IsTrue(result.All(item => item.IsWorkspaceArtifact));
    }
}
