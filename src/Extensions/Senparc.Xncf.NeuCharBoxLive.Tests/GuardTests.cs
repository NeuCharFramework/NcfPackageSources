/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：GuardTests.cs
    文件功能描述：NeuCharBox Live 测试：GuardTests


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.FileProviders;
using Senparc.Xncf.NeuCharBoxLive.Tests.Fakes;

namespace Senparc.Xncf.NeuCharBoxLive.Tests;

/// <summary>
/// The tunnel guard (spec 4.4, global constraints): from outside, only /live/api and /live/api/* answer; everything
/// else is an empty 404. Requests go through TestServer.SendAsync, so each sets its own remote address, Host and headers.
/// </summary>
[TestClass]
public class GuardTests
{
    /// <summary>NCF's admin, dynamic WebApi, installer, MCP routes, Swagger, and paths that only look like the API.</summary>
    static readonly string[] Blocked =
    {
        "/Admin", "/admin/x", "/Admin/NeuCharBoxLive/Codes", "/api/Senparc.Xncf.Installer/x", "/mcp-agentsmanager",
        "/mcp-agentsmanager/sse", "/swagger", "/swagger/index.html", "/live", "/live/", "/live/apix", "/live/apix/health",
        "/liveapi/health", "/", "/Install",
    };

    /// <summary>What ngrok's local agent sends: it connects from loopback and adds X-Forwarded-For.</summary>
    static Dictionary<string, string> Tunnel() => new() { ["X-Forwarded-For"] = "203.0.113.9" };

    [TestMethod]
    public async Task Guard_ExternalRequests_OnlyLiveApiPasses()
    {
        await using var h = await LiveHost.StartAsync();

        // Through the tunnel: the public Host, and a spoofed localhost Host, with and without a port.
        foreach (var host in new[] { "x.ngrok-free.app", "localhost", "localhost:5660", "127.0.0.1:5660", "[::1]:5660" })
        {
            foreach (var path in Blocked)
            foreach (var method in new[] { "GET", "POST", "OPTIONS" })
            {
                var r = await h.RawAsync(method, path, "127.0.0.1", host, Tunnel());
                Assert.AreEqual(404, r.Status, $"{method} {path} via Host {host}");
                Assert.AreEqual("", r.Body, $"{method} {path} via Host {host}: the guard's 404 is empty");
            }
            foreach (var path in new[] { "/live/api/health", "/LIVE/API/health", "/Live/Api/health" })
            {
                var r = await h.RawAsync("GET", path, "127.0.0.1", host, Tunnel());
                Assert.AreEqual(200, r.Status, $"{path} via Host {host}");
                StringAssert.Contains(r.Body, "\"ok\":true", $"{path} via Host {host} reached the controller");
            }
        }

        // Each forwarding header alone marks a loopback request as the tunnel's.
        foreach (var header in new[] { "X-Forwarded-For", "X-Forwarded-Host", "X-Forwarded-Proto", "Forwarded" })
        {
            var r = await h.RawAsync("GET", "/Admin", "127.0.0.1", "localhost:5660", new Dictionary<string, string> { [header] = "x" });
            Assert.AreEqual(404, r.Status, header);
        }

        // Local: loopback, a localhost Host, no X-Forwarded-*: the guard lets everything through to the host.
        foreach (var (ip, host) in new[] { ("127.0.0.1", "localhost:5660"), ("127.0.0.1", "LOCALHOST"), ("127.0.0.1", "127.0.0.1:5660"), ("::1", "[::1]:5660"), ("::ffff:127.0.0.1", "localhost:5660") })
        foreach (var path in new[] { "/anything-else", "/Admin", "/live/apix" })
        {
            var r = await h.RawAsync("GET", path, ip, host);
            Assert.AreEqual(200, r.Status, $"{ip} {host} {path}");
            Assert.AreEqual(LiveHost.FellThrough, r.Body, $"{ip} {host} {path} fell through the guard");
        }
        Assert.AreEqual(200, (await h.RawAsync("GET", "/live/api/health", "127.0.0.1", "localhost:5660")).Status);

        // Not loopback (a LAN machine), even with a localhost Host; no remote address at all; a lookalike Host.
        Assert.AreEqual(404, (await h.RawAsync("GET", "/Admin", "192.168.1.5", "localhost")).Status);
        Assert.AreEqual(404, (await h.RawAsync("GET", "/Admin", "192.168.1.5", "localhost:5660")).Status);
        Assert.AreEqual(404, (await h.RawAsync("GET", "/Admin", null, "localhost")).Status);
        Assert.AreEqual(404, (await h.RawAsync("GET", "/Admin", "127.0.0.1", "localhost.evil.example")).Status);
        Assert.AreEqual(404, (await h.RawAsync("GET", "/Admin", "127.0.0.1", "x.ngrok-free.app")).Status);
        Assert.AreEqual(200, (await h.RawAsync("GET", "/live/api/health", "192.168.1.5", "localhost")).Status);
    }

    /// <summary>An unmatched /live/api path answers the contract's JSON 404, never the host's pages, from outside or local.</summary>
    [TestMethod]
    public async Task UnmatchedLiveApi_IsJson404NotHostFallthrough()
    {
        await using var h = await LiveHost.StartAsync();
        foreach (var path in new[] { "/live/api", "/live/api/", "/live/api/nope", "/Live/Api/nope/deeper", "/live/api/session/x/bogus" })
        {
            foreach (var (host, hdr) in new[] { ("x.ngrok-free.app", Tunnel()), ("localhost:5660", (Dictionary<string, string>?)null) })
            {
                var r = await h.RawAsync("GET", path, "127.0.0.1", host, hdr);
                Assert.AreEqual(404, r.Status, $"{path} via {host}");
                StringAssert.Contains(r.Body, "\"error\":\"no_request\"", $"{path} via {host}");
                Assert.AreNotEqual(LiveHost.FellThrough, r.Body);
            }
        }
    }

    /// <summary>
    /// Paths that start with /live/api but aren't the API: dot, backslash, percent, colon, empty and non-ASCII segments.
    /// Kestrel decodes %5C to a backslash, and on Windows the host's static files split on it (see the next test), so
    /// from outside only clean segments ([A-Za-z0-9_-]+) pass, and only GET, POST and OPTIONS.
    /// </summary>
    static readonly string[] Dirty =
    {
        @"/live/api/..\..\css\site.css", "/live/api/..%2F..%2FAdmin", "/live/api/%2e%2e/Admin", "/live/api/./health",
        "/live/api//health", "/live/api/health.", "/live/api/..", "/live/api/.", "/live/api/health\\", @"/live/api/x\health",
        "/live/api/health%00", "/live/api/health:stream", "/live/api/he alth", "/live/api/h" + (char)0xE9 + "alth",
        "/live/api/" + (char)0x212A + "elvin", "/live/api/health" + (char)10, "/live/api/health" + (char)0,
        "/live/api/sessions/abc;x/events", "/live/api/sessions/abc/events/..", "/live/api///", "/live/api/~admin",
        "/live/api/health~", "/l" + (char)0x131 + "ve/api/health",
        // The same tricks once decoded.
        "/live/api/../Admin", "/live/api/../../css/site.css", "/live/api/x/../../Admin",
    };

    [TestMethod]
    public async Task Guard_ExternalOnlyCleanLiveApiPathsAndMethods()
    {
        await using var h = await LiveHost.StartAsync();
        foreach (var host in new[] { "x.ngrok-free.app", "localhost" })
        {
            foreach (var path in Dirty)
            foreach (var method in new[] { "GET", "POST", "OPTIONS" })
            {
                var r = await h.RawAsync(method, path, "127.0.0.1", host, Tunnel());
                Assert.AreEqual(404, r.Status, $"{method} {path} via Host {host}");
                Assert.AreEqual("", r.Body, $"{method} {path} via Host {host}: the guard's 404 is empty");
            }
            foreach (var method in new[] { "HEAD", "PUT", "DELETE", "PATCH", "TRACE", "CONNECT", "PROPFIND" })
            {
                var r = await h.RawAsync(method, "/live/api/health", "127.0.0.1", host, Tunnel());
                Assert.AreEqual(404, r.Status, $"{method} via Host {host}");
                Assert.AreEqual("", r.Body, method);
            }
        }

        // Clean paths still pass: a trailing slash, the bare prefix, ids made of the allowed characters, and lowercase methods.
        var ok = await h.RawAsync("GET", "/live/api/health/", "127.0.0.1", "x.ngrok-free.app", Tunnel());
        Assert.AreEqual(200, ok.Status);
        StringAssert.Contains(ok.Body, "\"ok\":true");
        Assert.AreEqual(200, (await h.RawAsync("get", "/live/api/health", "127.0.0.1", "x.ngrok-free.app", Tunnel())).Status);
        foreach (var path in new[] { "/live/api", "/live/api/", "/LIVE/API/" })
            StringAssert.Contains((await h.RawAsync("GET", path, "127.0.0.1", "x.ngrok-free.app", Tunnel())).Body, "\"error\":\"no_request\"",
                $"{path} passes the guard; no route answers it, so the API fallback does");
        var noCode = await h.RawAsync("GET", "/live/api/sessions/abc_DEF-123/events", "127.0.0.1", "x.ngrok-free.app", Tunnel());
        Assert.AreEqual(401, noCode.Status, "reached the controller");
        StringAssert.Contains(noCode.Body, "\"error\":\"bad_code\"");
        // Local requests are not limited to clean paths or methods. This one passes the guard; no route answers it,
        // so the API fallback gives the JSON 404, not the host's pages.
        StringAssert.Contains((await h.RawAsync("PUT", "/live/api/health.", "127.0.0.1", "localhost:5660")).Body, "\"error\":\"no_request\"");
    }

    [TestMethod]
    public async Task Guard_BackslashTraversal_NeverReachesStaticFiles()
    {
        if (!OperatingSystem.IsWindows()) Assert.Inconclusive(@"PhysicalFileProvider treats '\' as a separator only on Windows.");
        var root = Directory.CreateTempSubdirectory("ncb-guard-").FullName;
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "css"));
            await File.WriteAllTextAsync(Path.Combine(root, "css", "site.css"), "body{color:red}");
            using var files = new PhysicalFileProvider(root);
            await using var h = await LiveHost.StartAsync(afterGuard: app => app.UseStaticFiles(new StaticFileOptions { FileProvider = files }));

            // Locally the host serves its files, and the backslash path reaches the same file: the escape is real.
            Assert.AreEqual((200, "body{color:red}"), await h.RawAsync("GET", "/css/site.css", "127.0.0.1", "localhost:5660"));
            Assert.AreEqual((200, "body{color:red}"), await h.RawAsync("GET", @"/live/api/..\..\css\site.css", "127.0.0.1", "localhost:5660"));

            // Through the tunnel (Kestrel has already decoded %5C to '\') it never gets there.
            foreach (var path in new[] { @"/live/api/..\..\css\site.css", @"/live/api/x\..\..\..\css\site.css", @"/LIVE/API/..\..\css/site.css" })
            foreach (var host in new[] { "x.ngrok-free.app", "localhost" })
                Assert.AreEqual((404, ""), await h.RawAsync("GET", path, "127.0.0.1", host, Tunnel()), $"{path} via Host {host}");
        }
        finally { Directory.Delete(root, recursive: true); }
    }
}
