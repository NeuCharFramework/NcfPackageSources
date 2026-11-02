using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Functions.Parameters;
using Senparc.Xncf.Sandbox.Abstractions;
using Senparc.Xncf.Sandbox.Application.AppServices;
using Senparc.Xncf.Sandbox.Application.DTOs.Request;

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
            "LabUploadFiles",
            "LabDownloadFile",
            "LabListFiles",
            "LabCreateNotebook"
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

    [TestMethod]
    public void AliasAndCreateFunctions_AreRegistered()
    {
        var methods = typeof(SandboxAppService)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(method => method.GetCustomAttribute<FunctionRenderAttribute>() != null)
            .ToDictionary(method => method.Name, StringComparer.Ordinal);

        Assert.IsTrue(methods.ContainsKey("Create"), "Missing FunctionRender method: Create");
        Assert.IsTrue(methods.ContainsKey("UpdateAlias"), "Missing FunctionRender method: UpdateAlias");
    }

    [TestMethod]
    public void CreateRequestTemplateOptions_ExposeProtocolValues()
    {
        var request = new Sandbox_CreateRequest();

        CollectionAssert.AreEqual(
            new[]
            {
                SandboxTemplateKeys.PythonExec,
                SandboxTemplateKeys.CsharpExec,
                SandboxTemplateKeys.JupyterPython,
                SandboxTemplateKeys.JupyterCsharp
            },
            request.TemplateOptions.Items.Select(item => item.Value).ToArray());
    }

    [TestMethod]
    public void BatchUploadRequest_ExposesFileParameterMetadata()
    {
        var property = typeof(Sandbox_LabUploadFilesRequest).GetProperty(nameof(Sandbox_LabUploadFilesRequest.Files));

        Assert.IsNotNull(property);
        var attribute = property.GetCustomAttributes(typeof(FunctionParameterUiAttribute), inherit: true)
            .Cast<FunctionParameterUiAttribute>()
            .Single();
        Assert.AreEqual(ParameterType.File, attribute.ParameterType);
        Assert.IsTrue(attribute.AllowMultiple);
        Assert.AreEqual(16, attribute.MaxFiles);
    }

}
