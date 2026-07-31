/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：BuildXncfAppService.Preview.cs
    文件功能描述：XNCF 独立预览管理接口


    创建标识：Senparc - 20260801

----------------------------------------------------------------*/

using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Xncf.XncfBuilder.Domain.Services.Preview;
using Senparc.Xncf.XncfBuilder.OHS.PL;
using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.XncfBuilder.OHS.Local
{
    public partial class BuildXncfAppService
    {
        [FunctionRender(typeof(XncfBuilderResource), "Function.XncfBuilder.Preview.Name", "Function.XncfBuilder.Preview.Description", typeof(Register))]
        public async Task<StringAppResponse> Preview(BuildXncf_PreviewRequest request)
        {
            return await this.GetStringResponseAsync(async (response, logger) =>
            {
                if (request == null)
                {
                    response.Success = false;
                    response.Data = XncfBuilderResource.Get("XncfBuilder.Preview.RequestMissing");
                    return null;
                }

                try
                {
                    var previewService = ServiceProvider.GetRequiredService<IXncfPreviewService>();
                    var session = await previewService.StartAsync(
                        new XncfPreviewStartOptions
                        {
                            SolutionFilePath = request.SlnFilePath,
                            ModuleProjectName = request.ModuleProjectName,
                            Port = request.Port,
                            StartupTimeoutSeconds = request.StartupTimeoutSeconds,
                            EnvironmentName = request.EnvironmentName
                        },
                        message => logger.Append(message),
                        this.CancellationToken).ConfigureAwait(false);

                    response.Success = true;
                    response.Data = BuildPreviewStartedHtml(session);
                }
                catch (Exception ex)
                {
                    response.Success = false;
                    response.StateCode = 101;
                    response.ErrorMessage = ex.Message;
                    response.Data = WebUtility.HtmlEncode(ex.Message);
                    logger.Append($"Preview Exception: {ex.Message}");
                }

                return null;
            }).ConfigureAwait(false);
        }

        [FunctionRender(typeof(XncfBuilderResource), "Function.XncfBuilder.PreviewStatus.Name", "Function.XncfBuilder.PreviewStatus.Description", typeof(Register))]
        public async Task<StringAppResponse> PreviewStatus(BuildXncf_PreviewStatusRequest request)
        {
            return await this.GetStringResponseAsync((response, logger) =>
            {
                var previewService = ServiceProvider.GetRequiredService<IXncfPreviewService>();
                var sessions = previewService.GetSessions(request?.IncludeOutput == true);
                response.Success = true;

                if (sessions.Count == 0)
                {
                    response.Data = XncfBuilderResource.Get("XncfBuilder.Preview.NoSessions");
                    return Task.FromResult<string>(null);
                }

                var html = new StringBuilder();
                foreach (var session in sessions)
                {
                    html.Append("<p><strong>")
                        .Append(WebUtility.HtmlEncode(session.ModuleProjectName))
                        .Append("</strong><br />")
                        .Append(WebUtility.HtmlEncode(session.SessionId))
                        .Append("<br /><a href=\"")
                        .Append(WebUtility.HtmlEncode(session.Url))
                        .Append("\" target=\"_blank\" rel=\"noopener noreferrer\">")
                        .Append(WebUtility.HtmlEncode(session.Url))
                        .Append("</a><br />")
                        .Append(session.IsRunning
                            ? XncfBuilderResource.Get("XncfBuilder.Preview.Running")
                            : XncfBuilderResource.Get("XncfBuilder.Preview.Stopped"))
                        .Append(" · PID ")
                        .Append(session.ProcessId)
                        .Append("</p>");

                    if (!string.IsNullOrWhiteSpace(session.RecentOutput))
                    {
                        html.Append("<pre>")
                            .Append(WebUtility.HtmlEncode(session.RecentOutput))
                            .Append("</pre>");
                    }
                }

                response.Data = html.ToString();
                return Task.FromResult<string>(null);
            }).ConfigureAwait(false);
        }

        [FunctionRender(typeof(XncfBuilderResource), "Function.XncfBuilder.StopPreview.Name", "Function.XncfBuilder.StopPreview.Description", typeof(Register))]
        public async Task<StringAppResponse> StopPreview(BuildXncf_StopPreviewRequest request)
        {
            return await this.GetStringResponseAsync(async (response, logger) =>
            {
                if (string.IsNullOrWhiteSpace(request?.SessionId))
                {
                    response.Success = false;
                    response.Data = XncfBuilderResource.Get("XncfBuilder.Preview.SessionRequired");
                    return null;
                }

                var previewService = ServiceProvider.GetRequiredService<IXncfPreviewService>();
                var stopped = await previewService.StopAsync(
                    request.SessionId,
                    message => logger.Append(message),
                    this.CancellationToken).ConfigureAwait(false);
                response.Success = stopped;
                response.Data = stopped
                    ? XncfBuilderResource.Get("XncfBuilder.Preview.StopSucceeded")
                    : XncfBuilderResource.Get("XncfBuilder.Preview.SessionNotFound");
                return null;
            }).ConfigureAwait(false);
        }

        private static string BuildPreviewStartedHtml(XncfPreviewSessionInfo session)
        {
            var url = WebUtility.HtmlEncode(session.Url);
            return $"{XncfBuilderResource.Get("XncfBuilder.Preview.StartSucceeded")}<br />" +
                   $"Session: {WebUtility.HtmlEncode(session.SessionId)}<br />" +
                   $"PID: {session.ProcessId}<br />" +
                   $"<a href=\"{url}\" target=\"_blank\" rel=\"noopener noreferrer\">{url}</a>";
        }
    }
}
