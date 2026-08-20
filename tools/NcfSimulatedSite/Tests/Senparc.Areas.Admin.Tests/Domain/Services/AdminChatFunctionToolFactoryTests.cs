using Microsoft.Extensions.AI;
using Senparc.Areas.Admin.Domain.Services;

namespace Senparc.Areas.Admin.Tests.Domain.Services;

[TestClass]
public class AdminChatFunctionToolFactoryTests
{
    [TestMethod]
    public async Task Create_BindsInstanceFunctionToItsPlugin()
    {
        var plugin = new RecordingPlugin();
        var method = typeof(RecordingPlugin).GetMethod(nameof(RecordingPlugin.Echo));

        var function = AdminChatFunctionToolFactory.Create(
            method,
            plugin,
            "Xncf_RecordingPlugin_Echo",
            "Returns the supplied value.");

        await function.InvokeAsync(new AIFunctionArguments
        {
            ["value"] = "AdminChat"
        });

        Assert.AreEqual("Xncf_RecordingPlugin_Echo", function.Name);
        Assert.AreEqual(1, plugin.InvocationCount);
        Assert.AreEqual("AdminChat", plugin.LastValue);
    }

    private sealed class RecordingPlugin
    {
        public int InvocationCount { get; private set; }
        public string LastValue { get; private set; }

        public string Echo(string value)
        {
            InvocationCount++;
            LastValue = value;
            return value;
        }
    }
}
