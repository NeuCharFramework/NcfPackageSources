/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：PipelineTests.cs
    文件功能描述：测试模块自带的 /live/api 分支：CORS、JSON 404/405，宿主其他路径不受影响


    创建标识：Senparc - 20261109

    修改标识：Senparc - 20261109
    修改描述：v0.1.0-preview1 新增管道测试：按 NCF 顺序搭建站点（模块的 UseXncfModule 在 UseRouting 之前），并接收原 GuardTests 中的 404 用例

----------------------------------------------------------------*/

using System.Net;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Senparc.Xncf.NeuCharBoxLive.Api;
using Senparc.Xncf.NeuCharBoxLive.Engine.Access;

namespace Senparc.Xncf.NeuCharBoxLive.Tests;

/// <summary>
/// The module's own /live/api branch (<see cref="LiveApiPipeline"/>), on a site built in NCF's order: the module's
/// UseXncfModule first, then UseRouting, UseAuthorization and the endpoints, with a stand-in for an NCF page at
/// GET /Admin/Index. Nothing the host maps outside /live/api may change.
/// </summary>
[TestClass]
public class PipelineTests
{
    const string Demo = "https://leozheng265.github.io";
    const string Evil = "https://evil.example";
    const string Admin = "admin";

    /// <summary>An NCF-ordered site. <see cref="Runtime"/> starts off (no_model) on the built-in origins; health still answers.</summary>
    sealed class Site : IAsyncDisposable
    {
        public StaticLiveRuntime Runtime { get; } = new(LiveSnapshot.Off(LiveOff.NoModel, LiveCors.BuiltIns));
        public IHost Host { get; private set; } = null!;
        public HttpClient Client { get; private set; } = null!;

        /// <param name="module">False builds the same site without the module's UseXncfModule, to compare against.</param>
        public static async Task<Site> StartAsync(bool module = true)
        {
            var site = new Site();
            var cfg = new ConfigurationBuilder().Build();
            site.Host = new HostBuilder()
                .ConfigureLogging(l => l.ClearProviders())
                .ConfigureWebHost(w => w
                    .UseTestServer()
                    .ConfigureServices(s =>
                    {
                        s.AddRouting();
                        s.AddAuthorization();
                        s.AddNcbLive(cfg, b => b.UseRuntime(site.Runtime));
                    })
                    .Configure(app =>
                    {
                        if (module) new Register().UseXncfModule(app, registerService: null!);
                        app.UseRouting();
                        app.UseAuthorization();
                        app.UseEndpoints(e =>
                        {
                            e.MapControllers();
                            e.MapGet("/Admin/Index", () => Admin);
                        });
                    }))
                .Build();
            await site.Host.StartAsync();
            site.Client = site.Host.GetTestServer().CreateClient();
            return site;
        }

        public async Task<HttpResponseMessage> SendAsync(string method, string path, string? origin = null,
            Action<HttpRequestMessage>? edit = null)
        {
            var req = new HttpRequestMessage(new HttpMethod(method), path);
            if (origin is not null) req.Headers.TryAddWithoutValidation("Origin", origin);
            edit?.Invoke(req);
            return await Client.SendAsync(req);
        }

        /// <summary>A browser's preflight: Origin, Access-Control-Request-Method and -Headers, and optionally -Private-Network.</summary>
        public Task<HttpResponseMessage> PreflightAsync(string path, string origin, string method = "POST",
            string headers = "authorization,content-type", bool privateNetwork = false) =>
            SendAsync("OPTIONS", path, origin, req =>
            {
                req.Headers.TryAddWithoutValidation("Access-Control-Request-Method", method);
                req.Headers.TryAddWithoutValidation("Access-Control-Request-Headers", headers);
                if (privateNetwork) req.Headers.TryAddWithoutValidation("Access-Control-Request-Private-Network", "true");
            });

        public async ValueTask DisposeAsync()
        {
            Client?.Dispose();
            if (Host is not null)
            {
                await Host.StopAsync();
                Host.Dispose();
            }
        }
    }

    static string[] Values(HttpResponseMessage m, string header) =>
        m.Headers.TryGetValues(header, out var v)
            ? v.SelectMany(x => x.Split(',')).Select(x => x.Trim()).Where(x => x.Length > 0).ToArray()
            : Array.Empty<string>();

    static void AssertNoCors(HttpResponseMessage m, string what)
    {
        foreach (var header in new[] { "Access-Control-Allow-Origin", "Access-Control-Allow-Methods", "Access-Control-Allow-Headers",
                     "Access-Control-Max-Age", "Access-Control-Allow-Credentials", "Access-Control-Allow-Private-Network" })
            Assert.AreEqual(0, Values(m, header).Length, $"{what}: no {header}");
    }

    static async Task AssertJsonError(HttpResponseMessage m, int status, string kind, string what)
    {
        Assert.AreEqual(status, (int)m.StatusCode, what);
        Assert.AreEqual("application/json", m.Content.Headers.ContentType?.MediaType, what);
        var body = JsonNode.Parse(await m.Content.ReadAsStringAsync())!;
        Assert.AreEqual(kind, (string?)body["error"], what);
        Assert.AreEqual(LiveErrors.All[kind].Message, (string?)body["message"], what);
    }

    // ---- CORS ----

    [TestMethod]
    public async Task Preflight_AllowedOrigin_Gets204WithCorsHeaders()
    {
        await using var site = await Site.StartAsync();
        using var p = await site.PreflightAsync("/live/api/sessions", Demo);
        Assert.AreEqual(204, (int)p.StatusCode);
        CollectionAssert.AreEqual(new[] { Demo }, Values(p, "Access-Control-Allow-Origin"));
        CollectionAssert.IsSubsetOf(new[] { "POST" }, Values(p, "Access-Control-Allow-Methods"));
        CollectionAssert.AreEquivalent(new[] { "GET", "POST", "OPTIONS" }, Values(p, "Access-Control-Allow-Methods"));
        CollectionAssert.IsSubsetOf(new[] { "authorization", "content-type" },
            Values(p, "Access-Control-Allow-Headers").Select(x => x.ToLowerInvariant()).ToList());
        CollectionAssert.AreEqual(new[] { "600" }, Values(p, "Access-Control-Max-Age"));
        Assert.AreEqual(0, Values(p, "Access-Control-Allow-Credentials").Length, "never credentials");
        Assert.AreEqual(0, Values(p, "Access-Control-Allow-Private-Network").Length, "not asked for");
        Assert.AreEqual("", await p.Content.ReadAsStringAsync());

        // Any route under /live/api, in any case (routing is case-insensitive), the SSE stream's GET included.
        foreach (var (path, method) in new[] { ("/live/api/health", "GET"), ("/LIVE/API/health", "GET"), ("/live/api/sessions/x/events", "GET") })
        {
            using var q = await site.PreflightAsync(path, Demo, method, "authorization,ngrok-skip-browser-warning");
            Assert.AreEqual(204, (int)q.StatusCode, path);
            CollectionAssert.AreEqual(new[] { Demo }, Values(q, "Access-Control-Allow-Origin"), path);
        }
    }

    [TestMethod]
    public async Task Preflight_PrivateNetwork_GetsAllowHeader()
    {
        await using var site = await Site.StartAsync();
        using var p = await site.PreflightAsync("/live/api/sessions", Demo, privateNetwork: true);
        Assert.AreEqual(204, (int)p.StatusCode);
        CollectionAssert.AreEqual(new[] { Demo }, Values(p, "Access-Control-Allow-Origin"));
        CollectionAssert.AreEqual(new[] { "true" }, Values(p, "Access-Control-Allow-Private-Network"));

        // Only on a preflight: a plain GET that carries the request header gets no allow header.
        using var get = await site.SendAsync("GET", "/live/api/health", Demo,
            req => req.Headers.TryAddWithoutValidation("Access-Control-Request-Private-Network", "true"));
        Assert.AreEqual(200, (int)get.StatusCode);
        Assert.AreEqual(0, Values(get, "Access-Control-Allow-Private-Network").Length);
    }

    [TestMethod]
    public async Task Preflight_DisallowedOrigin_NoCorsHeaders()
    {
        await using var site = await Site.StartAsync();
        foreach (var origin in new[] { Evil, "null", Demo + ".evil.example", "https://evil.leozheng265.github.io", "http://leozheng265.github.io",
                     Demo + "/", "http://localhost:8765" })
        {
            using var p = await site.PreflightAsync("/live/api/sessions", origin, privateNetwork: true);
            Assert.AreEqual(204, (int)p.StatusCode, $"{origin}: the module still answers the preflight itself");
            AssertNoCors(p, origin);
        }
    }

    [TestMethod]
    public async Task Get_AllowedOrigin_HasAcao_Disallowed_DoesNot()
    {
        await using var site = await Site.StartAsync();
        foreach (var path in new[] { "/live/api/health", "/LIVE/API/health", "/Live/Api/health" })
        {
            using var ok = await site.SendAsync("GET", path, Demo);
            Assert.AreEqual(200, (int)ok.StatusCode, path);
            StringAssert.Contains(await ok.Content.ReadAsStringAsync(), "\"ok\":true", $"{path} reached the controller");
            CollectionAssert.AreEqual(new[] { Demo }, Values(ok, "Access-Control-Allow-Origin"), path);
            Assert.AreEqual(0, Values(ok, "Access-Control-Allow-Credentials").Length, path);
        }

        using var evil = await site.SendAsync("GET", "/live/api/health", Evil);
        Assert.AreEqual(200, (int)evil.StatusCode);
        AssertNoCors(evil, Evil);

        using var none = await site.SendAsync("GET", "/live/api/health");
        Assert.AreEqual(200, (int)none.StatusCode);
        AssertNoCors(none, "no Origin");
    }

    [TestMethod]
    public async Task ExtraOrigin_AddedAtRuntime_TakesEffect()
    {
        await using var site = await Site.StartAsync();
        const string extra = "https://a.example";
        using (var before = await site.PreflightAsync("/live/api/sessions", extra))
            AssertNoCors(before, "not on the list yet");

        // No restart: the next request reads the new snapshot's origins.
        site.Runtime.Current = site.Runtime.Current with { Origins = LiveCors.BuiltIns.Append(extra).ToArray() };
        using (var after = await site.PreflightAsync("/live/api/sessions", extra))
        {
            Assert.AreEqual(204, (int)after.StatusCode);
            CollectionAssert.AreEqual(new[] { extra }, Values(after, "Access-Control-Allow-Origin"));
        }
        using (var get = await site.SendAsync("GET", "/live/api/health", extra))
            CollectionAssert.AreEqual(new[] { extra }, Values(get, "Access-Control-Allow-Origin"));
        using (var demo = await site.PreflightAsync("/live/api/sessions", Demo))
            CollectionAssert.AreEqual(new[] { Demo }, Values(demo, "Access-Control-Allow-Origin"), "the built-in stays");

        // And it goes again when the snapshot drops it.
        site.Runtime.Current = site.Runtime.Current with { Origins = LiveCors.BuiltIns };
        using (var gone = await site.PreflightAsync("/live/api/sessions", extra))
            AssertNoCors(gone, "dropped");
    }

    // ---- the host's own paths ----

    [TestMethod]
    public async Task NonLiveApiPath_Untouched()
    {
        await using var site = await Site.StartAsync();
        using (var page = await site.SendAsync("GET", "/Admin/Index", Demo))
        {
            Assert.AreEqual(200, (int)page.StatusCode);
            Assert.AreEqual(Admin, await page.Content.ReadAsStringAsync());
            AssertNoCors(page, "GET /Admin/Index");
        }
        using (var preflight = await site.PreflightAsync("/Admin/Index", Demo, "GET", privateNetwork: true))
        {
            AssertNoCors(preflight, "OPTIONS /Admin/Index");
            Assert.AreNotEqual(204, (int)preflight.StatusCode, "not answered by the module");
        }

        // Exactly what the same site answers without the module: the host's pages, its 404s and 405s, and paths that only
        // look like the API.
        await using var bare = await Site.StartAsync(module: false);
        var paths = new[] { "/Admin/Index", "/nope", "/", "/live", "/live/", "/live/apix", "/live/apix/health", "/liveapi/health", "/api/live/health" };
        foreach (var path in paths)
        foreach (var method in new[] { "GET", "POST", "PUT", "HEAD", "OPTIONS", "PREFLIGHT" })
        {
            var what = $"{method} {path}";
            async Task<(HttpStatusCode Status, string? Type, string Body, string[] Allow)> AnswerAsync(Site s)
            {
                using var m = method == "PREFLIGHT"
                    ? await s.PreflightAsync(path, Demo, "GET", privateNetwork: true)
                    : await s.SendAsync(method, path, Demo);
                AssertNoCors(m, what);
                return (m.StatusCode, m.Content.Headers.ContentType?.ToString(), await m.Content.ReadAsStringAsync(),
                    m.Content.Headers.Allow.ToArray());
            }
            var withModule = await AnswerAsync(site);
            var without = await AnswerAsync(bare);
            Assert.AreEqual(without.Status, withModule.Status, what);
            Assert.AreEqual(without.Type, withModule.Type, what);
            Assert.AreEqual(without.Body, withModule.Body, what);
            CollectionAssert.AreEqual(without.Allow, withModule.Allow, what);
        }
    }

    // ---- the contract's JSON errors ----

    /// <summary>
    /// Any /live/api path that no route matches is the contract's JSON 404 <c>no_request</c>, whatever the method, never
    /// the host's empty 404 or its pages (from GuardTests.UnmatchedLiveApi_IsJson404NotHostFallthrough).
    /// </summary>
    [TestMethod]
    public async Task UnknownLiveApiPath_JsonNotFound()
    {
        await using var site = await Site.StartAsync();
        using (var r = await site.SendAsync("GET", "/live/api/nope"))
            await AssertJsonError(r, 404, "no_request", "GET /live/api/nope");

        foreach (var (method, path) in new[]
                 {
                     ("GET", "/live/api"), ("GET", "/live/api/"), ("GET", "/Live/Api/nope/deeper"), ("GET", "/live/api/session/x/bogus"),
                     ("POST", "/live/api/nope"), ("PUT", "/live/api/health."), ("DELETE", "/live/api/nope"), ("OPTIONS", "/live/api/nope"),
                 })
        {
            using var r = await site.SendAsync(method, path);
            await AssertJsonError(r, 404, "no_request", $"{method} {path}");
        }

        // An allowed origin can read it; a HEAD response stays empty.
        using (var r = await site.SendAsync("GET", "/live/api/nope", Demo))
        {
            await AssertJsonError(r, 404, "no_request", "from the demo");
            CollectionAssert.AreEqual(new[] { Demo }, Values(r, "Access-Control-Allow-Origin"));
        }
        using (var head = await site.SendAsync("HEAD", "/live/api/nope"))
        {
            Assert.AreEqual(404, (int)head.StatusCode);
            Assert.AreEqual("", await head.Content.ReadAsStringAsync());
        }
    }

    [TestMethod]
    public async Task WrongMethod_Json405()
    {
        await using var site = await Site.StartAsync();
        foreach (var (method, path, allow) in new[]
                 {
                     ("PUT", "/live/api/health", "GET"), ("POST", "/live/api/health", "GET"), ("DELETE", "/live/api/health", "GET"),
                     ("GET", "/live/api/sessions", "POST"), ("PATCH", "/live/api/apply", "POST"),
                 })
        {
            var what = $"{method} {path}";
            using var r = await site.SendAsync(method, path, Demo);
            await AssertJsonError(r, 405, "bad_request", what);
            CollectionAssert.Contains(r.Content.Headers.Allow.ToList(), allow, what);
            CollectionAssert.AreEqual(new[] { Demo }, Values(r, "Access-Control-Allow-Origin"), what);
        }
        using var head = await site.SendAsync("HEAD", "/live/api/health");
        Assert.AreEqual(405, (int)head.StatusCode);
        Assert.AreEqual("", await head.Content.ReadAsStringAsync(), "a HEAD response has no body");
    }

    // ---- the hook ----

    [TestMethod]
    public void Hooked_IsTrue_AfterUseXncfModule()
    {
        using var sp = new ServiceCollection().BuildServiceProvider();
        var app = new ApplicationBuilder(sp);
        Assert.AreSame(app, new Register().UseXncfModule(app, registerService: null!));
        Assert.IsTrue(LiveApiPipeline.Hooked);
    }

    [TestMethod]
    public void IsLiveApi_ThePrefixAndBelow_AnyCase()
    {
        foreach (var path in new[] { "/live/api", "/live/api/", "/live/api/health", "/LIVE/API/health", "/Live/Api/sessions/x/events" })
            Assert.IsTrue(LiveApiPipeline.IsLiveApi(new PathString(path)), path);
        foreach (var path in new[] { "", "/", "/live", "/live/", "/live/apix", "/live/apix/health", "/liveapi", "/api/live", "/Admin/Index" })
            Assert.IsFalse(LiveApiPipeline.IsLiveApi(new PathString(path)), path);
    }

    /// <summary>
    /// AddNcbLive adds CORS services only: no named policy, no policy provider of its own, and the CORS options no longer
    /// resolve the live runtime. The host's own CORS configuration is left as it was.
    /// </summary>
    [TestMethod]
    public void AddNcbLive_AddsNoCorsPolicy_AndLeavesTheHostsCorsAlone()
    {
        var resolved = 0;
        var services = new ServiceCollection();
        services.AddCors(o => o.AddPolicy("HostPolicy", p => p.WithOrigins(Evil)));
        services.AddNcbLive(new ConfigurationBuilder().Build(), b => b.UseRuntime(_ =>
        {
            resolved++;
            return new StaticLiveRuntime(LiveSnapshot.Off(LiveOff.NoModel, LiveCors.BuiltIns));
        }));
        using var sp = services.BuildServiceProvider();

        var options = sp.GetRequiredService<IOptions<CorsOptions>>().Value;
        Assert.AreEqual(0, resolved, "the CORS options don't resolve the runtime");
        Assert.IsNull(options.GetPolicy("NcbLive"), "no policy of the module's");
        CollectionAssert.AreEqual(new[] { Evil }, options.GetPolicy("HostPolicy")!.Origins.ToList(), "the host's policy is untouched");
        Assert.IsNull(options.GetPolicy(options.DefaultPolicyName), "no default policy either");
        Assert.IsInstanceOfType<CorsService>(sp.GetRequiredService<ICorsService>());
        Assert.IsInstanceOfType<DefaultCorsPolicyProvider>(sp.GetRequiredService<ICorsPolicyProvider>());
        Assert.AreEqual(1, services.Count(d => d.ServiceType == typeof(ICorsPolicyProvider)));
    }
}
