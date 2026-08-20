using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Xncf.Sandbox.Abstractions;
using Senparc.Xncf.Sandbox.Domain.Models.DatabaseModel;
using Senparc.Xncf.Sandbox.Domain.Services;

namespace Senparc.Xncf.Sandbox.Tests;

[TestClass]
public class SandboxSessionStateTests
{
    [TestMethod]
    public void MarkStopped_ClearsRuntimeConnectionDetails()
    {
        var session = new SandboxSession(
            "session-1",
            1,
            SandboxTemplateKeys.JupyterPython,
            SandboxRuntimeKind.Docker,
            0.5,
            512,
            DateTime.UtcNow.AddHours(1));

        session.MarkRunning("container-1", 49152, "http://127.0.0.1:49152/lab", "token");
        session.MarkStopped("container missing");

        Assert.AreEqual(SandboxSessionStatus.Stopped, session.Status);
        Assert.IsNull(session.RuntimeHandle);
        Assert.IsNull(session.HostPort);
        Assert.IsNull(session.AccessUrl);
        Assert.IsNull(session.AccessToken);
        Assert.AreEqual("container missing", session.StatusMessage);
    }

    [TestMethod]
    public void SetExpiresAtUtc_Unlimited_TagsSessionInfoAsUnlimited()
    {
        var session = new SandboxSession(
            "session-2",
            1,
            SandboxTemplateKeys.JupyterPython,
            SandboxRuntimeKind.Docker,
            0.5,
            512,
            DateTime.UtcNow.AddHours(1));

        session.SetExpiresAtUtc(SandboxTtlPolicy.UnlimitedExpiresAtUtc);

        Assert.IsTrue(session.ToInfo().IsTtlUnlimited);
    }
}
