/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuCharWorkflowTests.cs
    文件功能描述：Workflow 自动保存设置与版本快照测试
----------------------------------------------------------------*/

using Senparc.Areas.Admin.Domain.Models.DatabaseModel;

namespace Senparc.Areas.Admin.Tests.Domain.Models;

[TestClass]
public class NeuCharWorkflowTests
{
    [TestMethod]
    public void Update_ShouldPersistAutoSaveMinutesAndClampRange()
    {
        var workflow = new NeuCharWorkflow("Workflow", 1);

        Assert.AreEqual(3, workflow.AutoSaveMinutes);

        workflow.Update("Workflow", null, "{}", false, "manual", "{}", null, -1);
        Assert.AreEqual(0, workflow.AutoSaveMinutes);

        workflow.Update("Workflow", null, "{}", false, "manual", "{}", null, 9999);
        Assert.AreEqual(1440, workflow.AutoSaveMinutes);
        Assert.AreEqual(2, workflow.Revision);
    }

    [TestMethod]
    public void Version_ShouldCaptureWorkflowAndNormalizeSaveSource()
    {
        var workflow = new NeuCharWorkflow("Workflow", 1);
        workflow.Update("Workflow", "说明", "{\"nodes\":[]}", true, "manual", "{}", null, 5);

        var version = new NeuCharWorkflowVersion(workflow, 2, "SHORTCUT");

        Assert.AreEqual(workflow.Revision, version.Revision);
        Assert.AreEqual(workflow.GraphJson, version.GraphJson);
        Assert.AreEqual(5, version.AutoSaveMinutes);
        Assert.AreEqual(2, version.AdminUserId);
        Assert.AreEqual("shortcut", version.SaveSource);
    }
}
