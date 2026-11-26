/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PhysicalFileAppBuilderExtensions.cs
    File description: PhysicalFileAppBuilderExtensions related implementation

    Created by: Senparc - 20211215

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.12.16 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.FileProviders;
using System.IO;
using System.Threading.Tasks;

namespace Senparc.Xncf.ReloadPage.OHS.Hubs
{
    public static class PhysicalFileAppBuilderExtensions
    {
        private static readonly PhysicalFileProvider _fileProvider = new PhysicalFileProvider(Directory.GetCurrentDirectory());

        /// <summary>
        /// Registers a background watcher for JavaScript, CSS, and Razor file changes.
        /// </summary>
        /// <param name="app">The ASP.NET Core application builder.</param>
        /// <param name="hubContext">The SignalR hub context used for notifications.</param>
        /// <returns>The same application builder.</returns>
        public static IApplicationBuilder UsePhysicalFile(this IApplicationBuilder app, IHubContext<ReloadPageHub> hubContext)
        {
            RegisterPhysical(hubContext);
            return app;
        }

        /// <summary>
        /// Starts the physical-file monitoring loop for the reload page hub.
        /// </summary>
        /// <param name="hubContext">The SignalR hub context receiving change notifications.</param>
        public static void RegisterPhysical(IHubContext<ReloadPageHub> hubContext)
        {
            Task.Run(() =>
            {
                var tcs = new TaskCompletionSource<object>();
                while (true)
                {
                    PhysicalFileAsync(hubContext).GetAwaiter().GetResult();
                }
            });
        }

        /// <summary>
        /// Waits for one watched file change and broadcasts a reload notification.
        /// </summary>
        /// <param name="hubContext">The SignalR hub context receiving change notifications.</param>
        /// <returns>A task completing after one change notification is observed.</returns>
        private static async Task PhysicalFileAsync(IHubContext<ReloadPageHub> hubContext)
        {
            var jsToken = _fileProvider.Watch("wwwroot/**/*.js");
            var cssToken = _fileProvider.Watch("wwwroot/**/*.css");
            var cshtmlToken = _fileProvider.Watch("**/*.cshtml");
            var tcs = new TaskCompletionSource<object>();
            //Legacy comment translated to English.
            jsToken.RegisterChangeCallback(state =>
            {
                ((TaskCompletionSource<object>)state).TrySetResult(null);
                hubContext.Clients.All.SendAsync("ReloadPage", "js文件发生变化");
            }, tcs);
            cssToken.RegisterChangeCallback(state =>
            {
                ((TaskCompletionSource<object>)state).TrySetResult(null);
                hubContext.Clients.All.SendAsync("ReloadPage", "css文件发生变化");
            }, tcs);
            cshtmlToken.RegisterChangeCallback(state =>
            {
                ((TaskCompletionSource<object>)state).TrySetResult(null);
                hubContext.Clients.All.SendAsync("ReloadPage", "cshtml文件发生变化");
            }, tcs);
            await tcs.Task.ConfigureAwait(false);
        }
    }
}
