using Microsoft.VisualStudio.TestTools.UnitTesting;
using NcfDesktopApp.GUI.Services;

namespace NcfDesktopApp.GUI.Tests;

[TestClass]
public sealed class EmbeddedWebViewTests
{
    [DataTestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("未启动")]
    [DataRow("file:///tmp/local.html")]
    public void TryGetNavigableUri_RejectsPlaceholderAndNonHttpValues(string? value)
    {
        Assert.IsFalse(WebNavigationPolicy.TryGetNavigableUri(value, out _));
    }

    [DataTestMethod]
    [DataRow("http://localhost:5001")]
    [DataRow("https://127.0.0.1:5001/path")]
    public void TryGetNavigableUri_AcceptsAbsoluteHttpUrls(string value)
    {
        Assert.IsTrue(WebNavigationPolicy.TryGetNavigableUri(value, out var uri));
        Assert.IsTrue(uri.IsAbsoluteUri);
    }
}
