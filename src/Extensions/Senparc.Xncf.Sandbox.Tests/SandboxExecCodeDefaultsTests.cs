using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Xncf.Sandbox.Abstractions;
using Senparc.Xncf.Sandbox.Domain.Services;

namespace Senparc.Xncf.Sandbox.Tests;

[TestClass]
public class SandboxExecCodeDefaultsTests
{
    [TestMethod]
    public void Normalize_CsharpWithPythonDefault_ReplacesSample()
    {
        var code = SandboxExecCodeDefaults.Normalize(
            SandboxTemplateKeys.CsharpExec,
            SandboxExecCodeDefaults.PythonHello);
        Assert.AreEqual(SandboxExecCodeDefaults.CsharpHello, code);
    }

    [TestMethod]
    public void Normalize_PythonKeepsDefault()
    {
        var code = SandboxExecCodeDefaults.Normalize(
            SandboxTemplateKeys.PythonExec,
            SandboxExecCodeDefaults.PythonHello);
        Assert.AreEqual(SandboxExecCodeDefaults.PythonHello, code);
    }

    [TestMethod]
    public void Normalize_CsharpCustomCode_Unchanged()
    {
        const string custom = "Console.WriteLine(1 + 1);";
        var code = SandboxExecCodeDefaults.Normalize(SandboxTemplateKeys.CsharpExec, custom);
        Assert.AreEqual(custom, code);
    }
}
