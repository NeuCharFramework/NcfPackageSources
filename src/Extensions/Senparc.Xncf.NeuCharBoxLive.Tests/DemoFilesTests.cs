/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：DemoFilesTests.cs
    文件功能描述：测试模块自带的演示页（/live/demo）：首页、脚本、指向本站的配置，宿主其他路径不受影响


    创建标识：Senparc - 20261111

    修改标识：Senparc - 20261111
    修改描述：v0.1.0-preview1 新增演示页测试

----------------------------------------------------------------*/

using System.Net;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Senparc.Xncf.NeuCharBoxLive.Api;

namespace Senparc.Xncf.NeuCharBoxLive.Tests;

/// <summary>
/// The demo pages the module serves itself (<see cref="LiveDemoFiles"/>), on a site built in NCF's order: the module's
/// part first, then UseRouting and the host's own endpoints, with a stand-in for an NCF page at GET /Admin/Index.
/// </summary>
[TestClass]
public class DemoFilesTests
{
    const string Admin = "admin";

    static string DemoDir([CallerFilePath] string here = "") =>
        Path.Combine(Path.GetDirectoryName(here)!, "..", "Senparc.Xncf.NeuCharBoxLive", "LiveDemo");

    static async Task<IHost> StartAsync(string? pathBase = null)
    {
        var host = new HostBuilder()
            .ConfigureLogging(l => l.ClearProviders())
            .ConfigureWebHost(w => w
                .UseTestServer()
                .ConfigureServices(s => s.AddRouting())
                .Configure(app =>
                {
                    if (pathBase is not null) app.UsePathBase(pathBase);
                    LiveDemoFiles.Use(app);
                    app.UseRouting();
                    app.UseEndpoints(e => e.MapGet("/Admin/Index", () => Admin));
                }))
            .Build();
        await host.StartAsync();
        return host;
    }

    [TestMethod]
    public async Task TheFolderWithoutItsSlash_RedirectsToTheSlash_KeepingTheQuery()
    {
        using var host = await StartAsync();
        var r = await host.GetTestClient().GetAsync("/live/demo?x=1");
        Assert.AreEqual(HttpStatusCode.Redirect, r.StatusCode);
        Assert.AreEqual("/live/demo/?x=1", r.Headers.Location!.OriginalString);

        using var based = await StartAsync("/ncf");
        r = await based.GetTestClient().GetAsync("/ncf/LIVE/Demo");
        Assert.AreEqual("/ncf/live/demo/", r.Headers.Location!.OriginalString, "under the site's path base");
    }

    [TestMethod]
    public async Task TheFolder_ServesThePage()
    {
        using var host = await StartAsync();
        var r = await host.GetTestClient().GetAsync("/live/demo/");
        Assert.AreEqual(HttpStatusCode.OK, r.StatusCode);
        Assert.AreEqual("text/html", r.Content.Headers.ContentType!.MediaType);
        Assert.AreEqual("no-cache", r.Headers.CacheControl!.ToString());
        var page = await r.Content.ReadAsStringAsync();
        StringAssert.Contains(page, "NeuCharBox");
        StringAssert.Contains(page, "js/main.js", "the page loads its script by a relative address");

        r = await host.GetTestClient().GetAsync("/live/demo/index.html");
        Assert.AreEqual(page, await r.Content.ReadAsStringAsync(), "index.html is the same page");
    }

    [TestMethod]
    public async Task ScriptsAndStyles_AreServedWithTheirTypes()
    {
        using var host = await StartAsync();
        var client = host.GetTestClient();
        foreach (var (path, type) in new[]
        {
            ("/live/demo/js/main.js", "text/javascript"),
            ("/live/demo/js/engine/live.js", "text/javascript"),
            ("/live/demo/vendor/three/three.module.js", "text/javascript"),
            ("/live/demo/css/app.css", "text/css"),
        })
        {
            var r = await client.GetAsync(path);
            Assert.AreEqual(HttpStatusCode.OK, r.StatusCode, path);
            Assert.AreEqual(type, r.Content.Headers.ContentType!.MediaType, path);
            Assert.IsTrue((await r.Content.ReadAsByteArrayAsync()).Length > 0, path);
        }
    }

    [TestMethod]
    public async Task TheCopysConfig_PointsAtTheSiteThePageCameFrom()
    {
        using var host = await StartAsync();
        var js = await host.GetTestClient().GetStringAsync("/live/demo/js/live-config.js");
        // Two folders above /live/demo/ is the site, where /live/api is: live mode is offered, and on the same origin.
        StringAssert.Contains(js, "export const LIVE_CONFIG = { apiBase: new URL('../../', globalThis.location.href).href };");
        Assert.IsFalse(js.Contains("apiBase: ''"), "the public page's empty API base (live not offered) is not in the copy");
    }

    [TestMethod]
    public void EveryFileOfTheCopy_IsEmbedded()
    {
        var dir = Path.GetFullPath(DemoDir());
        var onDisk = Directory.GetFiles(dir, "*", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(dir, f).Replace('\\', '/')).ToList();
        Assert.IsTrue(onDisk.Count > 50, "the demo copy is there: " + onDisk.Count);
        var files = LiveDemoFiles.Files();
        var missing = onDisk.Where(f => !files.GetFileInfo(f).Exists).ToList();
        Assert.AreEqual(0, missing.Count, "not embedded: " + string.Join(", ", missing));
    }

    [TestMethod]
    public async Task AnythingElse_GoesOnToTheHost()
    {
        using var host = await StartAsync();
        var client = host.GetTestClient();
        Assert.AreEqual(Admin, await client.GetStringAsync("/Admin/Index"), "the host's page is the host's");
        Assert.AreEqual(HttpStatusCode.NotFound, (await client.GetAsync("/live/demo/nope.js")).StatusCode, "a file the demo doesn't have");
        Assert.AreEqual(HttpStatusCode.NotFound, (await client.GetAsync("/live/demo/../../appsettings.json")).StatusCode, "nothing outside the folder");
        Assert.AreEqual(HttpStatusCode.NotFound, (await client.GetAsync("/live/demox/")).StatusCode, "/live/demox is not the folder");
        var post = await client.PostAsync("/live/demo/", new StringContent(""));
        Assert.AreNotEqual(HttpStatusCode.OK, post.StatusCode, "only GET and HEAD are answered");
    }
}
