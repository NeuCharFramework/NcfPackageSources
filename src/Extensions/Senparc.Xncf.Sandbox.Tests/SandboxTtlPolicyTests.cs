using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Xncf.Sandbox.Domain.Services;

namespace Senparc.Xncf.Sandbox.Tests;

[TestClass]
public class SandboxTtlPolicyTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 17, 0, 0, 0, DateTimeKind.Utc);

    [TestMethod]
    public void ResolveExpiresAtUtc_WithoutOverride_UsesTemplateDefault()
    {
        var expiresAtUtc = SandboxTtlPolicy.ResolveExpiresAtUtc(
            UtcNow,
            TimeSpan.FromMinutes(45),
            TimeSpan.FromHours(4),
            ttlMinutes: null,
            keepAlive: false);

        Assert.AreEqual(UtcNow.AddMinutes(45), expiresAtUtc);
    }

    [TestMethod]
    public void ResolveExpiresAtUtc_KeepAlive_UsesUnlimitedSentinel()
    {
        var expiresAtUtc = SandboxTtlPolicy.ResolveExpiresAtUtc(
            UtcNow,
            TimeSpan.FromMinutes(45),
            TimeSpan.FromHours(4),
            ttlMinutes: null,
            keepAlive: true);

        Assert.IsTrue(SandboxTtlPolicy.IsUnlimited(expiresAtUtc));
    }

    [TestMethod]
    public void ResolveExpiresAtUtc_FiniteTtlAboveLimit_Throws()
    {
        var exception = Assert.ThrowsException<InvalidOperationException>(() =>
            SandboxTtlPolicy.ResolveExpiresAtUtc(
                UtcNow,
                TimeSpan.FromMinutes(45),
                TimeSpan.FromHours(4),
                ttlMinutes: 241,
                keepAlive: false));

        StringAssert.Contains(exception.Message, "240");
    }
}
