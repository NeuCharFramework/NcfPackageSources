using Senparc.Xncf.AgentsManager.Domain.Services;
using System.Reflection;

namespace Senparc.Xncf.AgentsManager.Domain.Services.Tests;

[TestClass]
public class ChatGroupWorkflowExecutorIdentityTests
{
    [TestMethod]
    public void WorkflowExecutorId_NormalizesChineseRemoteA2AIdentityLikeMaf()
    {
        var method = typeof(ChatGroupService).GetMethod(
            "BuildWorkflowExecutorId",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(method);

        var executorId = method.Invoke(
            null,
            new object[] { "远程检索助手", "remote:42" }) as string;

        Assert.AreEqual("_remote_42", executorId);
    }

    [TestMethod]
    public void ChatRoundLimit_BlocksTheEleventhPersistedResponse()
    {
        var method = typeof(ChatGroupService).GetMethod(
            "CanPersistNextChatRound",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(method);

        Assert.IsTrue((bool)method.Invoke(null, new object[] { 9, 10 })!);
        Assert.IsFalse((bool)method.Invoke(null, new object[] { 10, 10 })!);
    }
}
