namespace Senparc.Xncf.AIKernel.Tests;

[TestClass]
public class AITokenProgressEventTests
{
    [TestMethod]
    public void DefaultStatus_IsRunning()
    {
        var evt = new AITokenProgressEvent();

        Assert.AreEqual(AITokenProgressStatus.Running, evt.Status);
    }

    [TestMethod]
    public void IsComplete_Running_IsFalse()
    {
        var evt = new AITokenProgressEvent { Status = AITokenProgressStatus.Running };

        Assert.IsFalse(evt.IsComplete);
    }

    [TestMethod]
    public void IsComplete_Completed_IsTrue()
    {
        var evt = new AITokenProgressEvent { Status = AITokenProgressStatus.Completed };

        Assert.IsTrue(evt.IsComplete);
    }

    [TestMethod]
    public void IsComplete_Failed_IsTrue()
    {
        var evt = new AITokenProgressEvent { Status = AITokenProgressStatus.Failed };

        Assert.IsTrue(evt.IsComplete);
    }

    [TestMethod]
    public void DefaultTimestamp_IsApproximatelyNow()
    {
        var before = DateTime.Now;
        var evt = new AITokenProgressEvent();
        var after = DateTime.Now;

        Assert.IsTrue(evt.Timestamp >= before && evt.Timestamp <= after);
    }
}
