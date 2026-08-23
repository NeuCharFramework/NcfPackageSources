using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Ncf.Core.AppServices;
using Senparc.Xncf.Sandbox.Application.AppServices;

namespace Senparc.Xncf.Sandbox.Tests;

[TestClass]
public class SandboxFunctionRenderTests
{
    [TestMethod]
    public void PersistentLabOperations_AreRegisteredAsAiCallableFunctions()
    {
        var expected = new[]
        {
            "LabExec",
            "LabUploadFile",
            "LabDownloadFile",
            "LabListFiles"
        };

        var methods = typeof(SandboxAppService)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(method => method.GetCustomAttribute<FunctionRenderAttribute>() != null)
            .ToDictionary(method => method.Name, StringComparer.Ordinal);

        foreach (var methodName in expected)
        {
            Assert.IsTrue(methods.TryGetValue(methodName, out var method), $"Missing FunctionRender method: {methodName}");
            Assert.IsTrue(
                method.GetCustomAttribute<FunctionRenderAttribute>()!.AllowAiInvocation,
                $"FunctionRender method is not AI callable: {methodName}");
        }
    }
}
