/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：ArchitectureTests.cs
    文件功能描述：测试引擎目录（Engine/）不得依赖 ASP.NET Core 与 NCF

    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using System.Text.RegularExpressions;

namespace Senparc.Xncf.NeuCharBoxLive.Tests;

/// <summary>Walks up from the test assembly to the module source (src/Extensions/Senparc.Xncf.NeuCharBoxLive).</summary>
static class RepoPaths
{
    public static string ModuleDir
    {
        get
        {
            for (var d = new DirectoryInfo(AppContext.BaseDirectory); d is not null; d = d.Parent)
            {
                var module = Path.Combine(d.FullName, "Senparc.Xncf.NeuCharBoxLive");
                if (Directory.Exists(Path.Combine(module, "Engine"))) return module;
            }
            Assert.Fail("Senparc.Xncf.NeuCharBoxLive/Engine not found above the test output");
            return "";
        }
    }
}

[TestClass]
public class ArchitectureTests
{
    // The engine is plain C# over Microsoft.Extensions.AI: no ASP.NET Core, no NCF/Senparc packages. It may use its own
    // namespaces (Senparc.Xncf.NeuCharBoxLive.Engine...).
    static readonly Regex ForbiddenUsing = new(
        @"^\s*(global\s+)?using\s+(static\s+)?(Microsoft\.AspNetCore|Senparc\.(?!Xncf\.NeuCharBoxLive\.Engine(\.|;)))",
        RegexOptions.Compiled);

    static List<string> Offenders(string engineDir) =>
        Directory.EnumerateFiles(engineDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => File.ReadLines(f).Any(l => ForbiddenUsing.IsMatch(l)))
            .ToList();

    [TestMethod]
    public void EngineFolder_HasNoAspNetOrNcfUsings()
    {
        var engine = Path.Combine(RepoPaths.ModuleDir, "Engine");
        CollectionAssert.AreEqual(Array.Empty<string>(), Offenders(engine));
    }

    [TestMethod]
    public void ForbiddenUsing_CatchesAspNetAndNcfButNotTheEnginesOwnNamespaces()
    {
        Assert.IsTrue(ForbiddenUsing.IsMatch("using Microsoft.AspNetCore.Http;"));
        Assert.IsTrue(ForbiddenUsing.IsMatch("global using Microsoft.AspNetCore.Builder;"));
        Assert.IsTrue(ForbiddenUsing.IsMatch("using Senparc.Ncf.Core;"));
        Assert.IsTrue(ForbiddenUsing.IsMatch("using Senparc.Xncf.NeuCharBoxLive.Api;"));
        Assert.IsTrue(ForbiddenUsing.IsMatch("using Senparc.Xncf.NeuCharBoxLive.Models;"));
        Assert.IsFalse(ForbiddenUsing.IsMatch("using Senparc.Xncf.NeuCharBoxLive.Engine.Access;"));
        Assert.IsFalse(ForbiddenUsing.IsMatch("using static Senparc.Xncf.NeuCharBoxLive.Engine.Sim.Foo;"));
        Assert.IsFalse(ForbiddenUsing.IsMatch("using Microsoft.Extensions.AI;"));
    }
}
