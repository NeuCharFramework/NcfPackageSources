using System;
using System.Reflection;

namespace Senparc.Xncf.AgentsManager.Domain.Services.Tests;

[TestClass]
public class AgentModelRequestDiagnosticsTests
{
    [TestMethod]
    public void ExtractProviderError_OnlyKeepsSafeFieldsAndRedactsSecrets()
    {
        var diagnosticsType = typeof(AgentTemplateRunner).Assembly.GetType(
            "Senparc.Xncf.AgentsManager.Domain.Services.AgentModelRequestDiagnostics",
            throwOnError: true);
        var method = diagnosticsType.GetMethod(
            "ExtractProviderError",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(method);

        var result = method.Invoke(
            null,
            new object[]
            {
                "{\"error\":{\"code\":\"invalid_request_error\",\"type\":\"bad_schema\",\"param\":\"tools\",\"message\":\"Authorization: Bearer super-secret-token at https://private.example/path\"},\"prompt\":\"PRIVATE\"}"
            }) as string;

        Assert.IsNotNull(result);
        StringAssert.Contains(result, "code=invalid_request_error");
        StringAssert.Contains(result, "type=bad_schema");
        StringAssert.Contains(result, "param=tools");
        Assert.IsFalse(result.Contains("super-secret-token", StringComparison.Ordinal));
        Assert.IsFalse(result.Contains("private.example", StringComparison.Ordinal));
        Assert.IsFalse(result.Contains("PRIVATE", StringComparison.Ordinal));
    }
}
