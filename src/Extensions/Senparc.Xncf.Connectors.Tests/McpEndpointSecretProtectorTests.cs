using Microsoft.AspNetCore.DataProtection;
using Senparc.Xncf.MCP.Domain.Services;

namespace Senparc.Xncf.Connectors.Tests;

[TestClass]
public class McpEndpointSecretProtectorTests
{
    [TestMethod]
    public void ProtectsAndRestoresAuthenticationConfiguration()
    {
        var protector = new McpEndpointSecretProtector(new EphemeralDataProtectionProvider());
        const string auth = """{"token":"secret-value"}""";

        var protectedValue = protector.Protect(auth);

        Assert.IsNotNull(protectedValue);
        Assert.AreNotEqual(auth, protectedValue);
        Assert.IsFalse(protectedValue.Contains("secret-value", StringComparison.Ordinal));
        Assert.AreEqual(auth, protector.Unprotect(protectedValue));
    }

    [TestMethod]
    public void ReadsLegacyPlainTextConfiguration()
    {
        var protector = new McpEndpointSecretProtector(new EphemeralDataProtectionProvider());
        const string legacy = """{"token":"legacy"}""";

        Assert.AreEqual(legacy, protector.Unprotect(legacy));
    }
}
