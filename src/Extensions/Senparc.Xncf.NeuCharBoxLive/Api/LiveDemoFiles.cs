/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：LiveDemoFiles.cs
    文件功能描述：模块自带的演示页（/live/demo），由内嵌的 LiveDemo 文件夹提供，与 /live/api 同源


    创建标识：Senparc - 20261111

    修改标识：Senparc - 20261111
    修改描述：v0.1.0-preview1 新增 LiveDemoFiles：站点自己提供演示页，无需公共演示页跨源访问本站

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;

namespace Senparc.Xncf.NeuCharBoxLive.Api;

/// <summary>
/// The demo's pages, served by the site itself at /live/demo/ from the module's embedded LiveDemo folder (a copy of the
/// demo repo made by its tools/sync-module.mjs, whose js/live-config.js points at this site). The page and /live/api
/// then share an origin: no CORS, no private-network prompt, and it works on any address the site has. Added from
/// <see cref="Register.UseXncfModule"/>, before the host's UseRouting. Only GET and HEAD under /live/demo are answered,
/// and only for a file that exists; everything else goes on to the host.
/// </summary>
public static class LiveDemoFiles
{
    public const string Prefix = "/live/demo";

    /// <summary>The embedded LiveDemo folder.</summary>
    internal static IFileProvider Files() => new ManifestEmbeddedFileProvider(typeof(LiveDemoFiles).Assembly, "LiveDemo");

    public static IApplicationBuilder Use(IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        var files = Files();
        // /live/demo → /live/demo/: the page's scripts are relative, so the folder needs its slash.
        app.Use(async (ctx, next) =>
        {
            var get = HttpMethods.IsGet(ctx.Request.Method) || HttpMethods.IsHead(ctx.Request.Method);
            if (get && ctx.Request.Path.Equals(Prefix, StringComparison.OrdinalIgnoreCase))
            {
                ctx.Response.Redirect(ctx.Request.PathBase + Prefix + "/" + ctx.Request.QueryString);
                return;
            }
            await next(ctx);
        });
        app.UseDefaultFiles(new DefaultFilesOptions { RequestPath = Prefix, FileProvider = files });
        app.UseStaticFiles(new StaticFileOptions
        {
            RequestPath = Prefix,
            FileProvider = files,
            // The page versions its own modules; the browser still asks each time, so an updated module shows at once.
            OnPrepareResponse = c => c.Context.Response.Headers.CacheControl = "no-cache",
        });
        return app;
    }
}
