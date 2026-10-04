/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：SandboxNotebookBuilderTests.cs
    文件功能描述：Notebook 构建与单元解析回归测试

    创建标识：Senparc - 20260918
    修改标识：Senparc - 20260918
    修改描述：v0.3.3 新增 Notebook 构建器回归测试

----------------------------------------------------------------*/

using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Xncf.Sandbox.Domain.Services;

namespace Senparc.Xncf.Sandbox.Tests;

[TestClass]
public class SandboxNotebookBuilderTests
{
    [TestMethod]
    public void Build_Python_SingleCodeCell()
    {
        var json = SandboxNotebookBuilder.Build("python", null, "print('hello')");

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.AreEqual(4, root.GetProperty("nbformat").GetInt32());
        Assert.AreEqual(5, root.GetProperty("nbformat_minor").GetInt32());
        Assert.AreEqual("python3", root.GetProperty("metadata").GetProperty("kernelspec").GetProperty("name").GetString());

        var cells = root.GetProperty("cells");
        Assert.AreEqual(1, cells.GetArrayLength());
        Assert.AreEqual("code", cells[0].GetProperty("cell_type").GetString());
        Assert.AreEqual("print('hello')", ReadSource(cells[0]));
        Assert.IsNull(cells[0].GetProperty("execution_count").ValueKind == JsonValueKind.Null ? null : "x");
        Assert.IsNotNull(cells[0].GetProperty("id").GetString());
    }

    [TestMethod]
    public void Build_CSharp_UsesDotnetInteractiveKernel()
    {
        var json = SandboxNotebookBuilder.Build("csharp", null, "Console.WriteLine(1);");

        using var document = JsonDocument.Parse(json);
        var kernelspec = document.RootElement.GetProperty("metadata").GetProperty("kernelspec");
        Assert.AreEqual("C# .NET SDK", kernelspec.GetProperty("name").GetString());
        Assert.AreEqual("CSharp", kernelspec.GetProperty("language").GetString());
    }

    [TestMethod]
    public void Build_WithTitle_InsertsMarkdownHeading()
    {
        var json = SandboxNotebookBuilder.Build("python", "Demo", "print('hello')");

        using var document = JsonDocument.Parse(json);
        var cells = document.RootElement.GetProperty("cells");
        Assert.AreEqual(2, cells.GetArrayLength());
        Assert.AreEqual("markdown", cells[0].GetProperty("cell_type").GetString());
        Assert.AreEqual("# Demo", ReadSource(cells[0]));
        Assert.AreEqual("code", cells[1].GetProperty("cell_type").GetString());
    }

    [TestMethod]
    public void Build_MarkdownMarker_CreatesMarkdownCell()
    {
        var json = SandboxNotebookBuilder.Build(
            "python",
            null,
            "print('a')\n# %% [markdown]\nExplain this\n# %%\nprint('b')");

        using var document = JsonDocument.Parse(json);
        var cells = document.RootElement.GetProperty("cells");
        Assert.AreEqual(3, cells.GetArrayLength());
        Assert.AreEqual("code", cells[0].GetProperty("cell_type").GetString());
        Assert.AreEqual("markdown", cells[1].GetProperty("cell_type").GetString());
        Assert.AreEqual("Explain this", ReadSource(cells[1]));
        Assert.AreEqual("code", cells[2].GetProperty("cell_type").GetString());
    }

    [TestMethod]
    public void Build_MultiLineSource_PreservesNewlines()
    {
        var json = SandboxNotebookBuilder.Build("python", null, "line1\nline2\n\n");

        using var document = JsonDocument.Parse(json);
        var source = document.RootElement.GetProperty("cells")[0].GetProperty("source");
        Assert.AreEqual(2, source.GetArrayLength());
        Assert.AreEqual("line1\n", source[0].GetString());
        Assert.AreEqual("line2", source[1].GetString());
    }

    [TestMethod]
    public void Build_CellIds_AreUnique()
    {
        var json = SandboxNotebookBuilder.Build("python", "Title", "a = 1\n# %%\nb = 2");

        using var document = JsonDocument.Parse(json);
        var ids = document.RootElement
            .GetProperty("cells")
            .EnumerateArray()
            .Select(cell => cell.GetProperty("id").GetString())
            .ToList();
        Assert.AreEqual(3, ids.Count);
        Assert.AreEqual(3, ids.Distinct(StringComparer.Ordinal).Count());
    }

    [TestMethod]
    public void Build_EmptyCells_Throws()
    {
        Assert.ThrowsException<InvalidOperationException>(() => SandboxNotebookBuilder.Build("python", null, "   "));
        Assert.ThrowsException<InvalidOperationException>(() => SandboxNotebookBuilder.Build("python", null, "# %%\n# %% [markdown]"));
    }

    [TestMethod]
    public void NormalizeLanguage_AcceptsAliasesAndRejectsUnknown()
    {
        Assert.AreEqual("python", SandboxNotebookBuilder.NormalizeLanguage("Python 3"));
        Assert.AreEqual("csharp", SandboxNotebookBuilder.NormalizeLanguage("C#"));
        Assert.AreEqual("csharp", SandboxNotebookBuilder.NormalizeLanguage("dotnet"));
        Assert.ThrowsException<InvalidOperationException>(() => SandboxNotebookBuilder.NormalizeLanguage("go"));
    }

    private static string ReadSource(JsonElement cell)
    {
        var source = cell.GetProperty("source");
        if (source.ValueKind == JsonValueKind.Array)
        {
            return string.Concat(source.EnumerateArray().Select(z => z.GetString()));
        }

        return source.GetString() ?? string.Empty;
    }
}
