namespace Senparc.Xncf.AIKernel.Tests;

[TestClass]
public class AITokenUsageSnapshotTests
{
    [TestMethod]
    public void IsEmpty_DefaultSnapshot_IsTrue()
    {
        Assert.IsTrue(new AITokenUsageSnapshot().IsEmpty);
    }

    [TestMethod]
    public void IsEmpty_AnyValue_IsFalse()
    {
        Assert.IsFalse(new AITokenUsageSnapshot { InputTokens = 1 }.IsEmpty);
        Assert.IsFalse(new AITokenUsageSnapshot { OutputTokens = 1 }.IsEmpty);
        Assert.IsFalse(new AITokenUsageSnapshot { TotalTokens = 1 }.IsEmpty);
        Assert.IsFalse(new AITokenUsageSnapshot { CachedInputTokens = 1 }.IsEmpty);
        Assert.IsFalse(new AITokenUsageSnapshot { ReasoningTokens = 1 }.IsEmpty);
    }

    [TestMethod]
    public void Empty_ReturnsFreshInstance_EachCall()
    {
        var first = AITokenUsageSnapshot.Empty;
        var second = AITokenUsageSnapshot.Empty;

        Assert.AreNotSame(first, second);
        Assert.IsTrue(first.IsEmpty);
    }

    [TestMethod]
    public void Normalize_ComputesTotalFromInputAndOutput_WhenTotalMissing()
    {
        var snapshot = new AITokenUsageSnapshot { InputTokens = 30, OutputTokens = 12 };

        snapshot.Normalize();

        Assert.AreEqual(42, snapshot.TotalTokens);
    }

    [TestMethod]
    public void Normalize_KeepsExistingTotal_WhenPositive()
    {
        var snapshot = new AITokenUsageSnapshot { InputTokens = 30, OutputTokens = 12, TotalTokens = 100 };

        snapshot.Normalize();

        Assert.AreEqual(100, snapshot.TotalTokens);
    }

    [TestMethod]
    public void Normalize_ClampsNegativeValues_ToZero()
    {
        var snapshot = new AITokenUsageSnapshot
        {
            InputTokens = -5,
            OutputTokens = -3,
            CachedInputTokens = -1,
            ReasoningTokens = -2
        };

        snapshot.Normalize();

        Assert.AreEqual(0, snapshot.InputTokens);
        Assert.AreEqual(0, snapshot.OutputTokens);
        Assert.AreEqual(0, snapshot.CachedInputTokens);
        Assert.AreEqual(0, snapshot.ReasoningTokens);
        Assert.AreEqual(0, snapshot.TotalTokens);
    }

    [TestMethod]
    public void Normalize_NegativeTotal_FallsBackToInputPlusOutput()
    {
        var snapshot = new AITokenUsageSnapshot { InputTokens = 30, OutputTokens = 12, TotalTokens = -10 };

        snapshot.Normalize();

        Assert.AreEqual(42, snapshot.TotalTokens);
    }
}
