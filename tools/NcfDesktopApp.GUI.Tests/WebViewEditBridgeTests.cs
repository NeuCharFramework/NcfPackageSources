using Microsoft.VisualStudio.TestTools.UnitTesting;
using NcfDesktopApp.GUI.Services;

namespace NcfDesktopApp.GUI.Tests;

[TestClass]
public sealed class WebViewEditBridgeTests
{
    [TestMethod]
    public void ScriptBridge_IsDisabledForMacOSCallbackSafety()
    {
        Assert.IsFalse(WebViewEditBridge.IsScriptBridgeSupportedForPlatform(isMacOS: true));
    }

    [TestMethod]
    public void ScriptBridge_RemainsEnabledForOtherPlatforms()
    {
        Assert.IsTrue(WebViewEditBridge.IsScriptBridgeSupportedForPlatform(isMacOS: false));
    }

    [TestMethod]
    public void NativeMacSelectors_UseStandardResponderActions()
    {
        Assert.AreEqual("cut:", WebViewEditBridge.GetNativeMacSelector(WebViewEditBridge.EditCommand.Cut));
        Assert.AreEqual("copy:", WebViewEditBridge.GetNativeMacSelector(WebViewEditBridge.EditCommand.Copy));
        Assert.AreEqual("paste:", WebViewEditBridge.GetNativeMacSelector(WebViewEditBridge.EditCommand.Paste));
        Assert.AreEqual("selectAll:", WebViewEditBridge.GetNativeMacSelector(WebViewEditBridge.EditCommand.SelectAll));
        Assert.IsNull(WebViewEditBridge.GetNativeMacSelector(WebViewEditBridge.EditCommand.None));
    }
}
