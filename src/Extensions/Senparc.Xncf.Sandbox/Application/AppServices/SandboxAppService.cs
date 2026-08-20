/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：SandboxAppService.cs
    文件功能描述：沙箱 Function / OHS 入口

    创建标识：Senparc - 20260808

----------------------------------------------------------------*/

using System.Net;
using System.Text;
using Senparc.Ncf.Core.AppServices;
using Senparc.Xncf.Sandbox.Abstractions;
using Senparc.Xncf.Sandbox.Application.DTOs.Request;
using Senparc.Xncf.Sandbox.Domain.Services;

namespace Senparc.Xncf.Sandbox.Application.AppServices;

public class SandboxAppService : AppServiceBase
{
    private readonly SandboxOrchestrator _orchestrator;

    public SandboxAppService(IServiceProvider serviceProvider, SandboxOrchestrator orchestrator)
        : base(serviceProvider)
    {
        _orchestrator = orchestrator;
    }

    [FunctionRender(typeof(SandboxResource), "Function.Sandbox.Create.Name", "Function.Sandbox.Create.Description", typeof(Register))]
    public async Task<StringAppResponse> Create(Sandbox_CreateRequest request)
    {
        return await this.GetStringResponseAsync(async (response, logger) =>
        {
            var runtime = Enum.TryParse<SandboxRuntimeKind>(request.RuntimeKind, true, out var kind)
                ? kind
                : SandboxRuntimeKind.Docker;

            logger.Append($"创建沙箱 Template={request.TemplateKey}, Runtime={runtime}");
            var info = await _orchestrator.CreateAsync(ownerUserId: 0, request.TemplateKey, runtime).ConfigureAwait(false);
            logger.Append($"SessionId={info.SessionId}, Status={info.Status}");
            if (!string.IsNullOrWhiteSpace(info.AccessUrl))
            {
                logger.Append($"AccessUrl={info.AccessUrl}");
            }

            response.Data = FormatSession(info);
            return null;
        });
    }

    [FunctionRender(typeof(SandboxResource), "Function.Sandbox.List.Name", "Function.Sandbox.List.Description", typeof(Register))]
    public async Task<StringAppResponse> List(Sandbox_ListRequest request)
    {
        return await this.GetStringResponseAsync(async (response, logger) =>
        {
            var list = await _orchestrator.ListAsync().ConfigureAwait(false);
            logger.Append($"共 {list.Count} 条");
            var sb = new StringBuilder();
            foreach (var item in list)
            {
                sb.Append(FormatSession(item)).Append("<hr/>");
            }

            response.Data = sb.Length == 0 ? "暂无会话" : sb.ToString();
            return null;
        });
    }

    [FunctionRender(typeof(SandboxResource), "Function.Sandbox.Status.Name", "Function.Sandbox.Status.Description", typeof(Register))]
    public async Task<StringAppResponse> Status(Sandbox_SessionIdRequest request)
    {
        return await this.GetStringResponseAsync(async (response, logger) =>
        {
            var info = await _orchestrator.GetAsync(request.SessionId).ConfigureAwait(false);
            if (info == null)
            {
                response.Success = false;
                response.ErrorMessage = "会话不存在";
                return null;
            }

            response.Data = FormatSession(info);
            return null;
        });
    }

    [FunctionRender(typeof(SandboxResource), "Function.Sandbox.Execute.Name", "Function.Sandbox.Execute.Description", typeof(Register))]
    public async Task<StringAppResponse> Exec(Sandbox_ExecRequest request)
    {
        return await this.GetStringResponseAsync(async (response, logger) =>
        {
            var result = await _orchestrator.ExecAsync(request.SessionId, request.Code).ConfigureAwait(false);
            logger.Append($"ExitCode={result.ExitCode}");
            response.Data =
                $"ExitCode: {result.ExitCode}<br/><b>stdout</b><pre>{WebUtility.HtmlEncode(result.StdOut)}</pre><b>stderr</b><pre>{WebUtility.HtmlEncode(result.StdErr)}</pre>";
            return null;
        });
    }

    [FunctionRender(typeof(SandboxResource), "Function.Sandbox.Destroy.Name", "Function.Sandbox.Destroy.Description", typeof(Register))]
    public async Task<StringAppResponse> Destroy(Sandbox_SessionIdRequest request)
    {
        return await this.GetStringResponseAsync(async (response, logger) =>
        {
            await _orchestrator.DestroyAsync(request.SessionId).ConfigureAwait(false);
            logger.Append($"已销毁 {request.SessionId}");
            response.Data = $"已销毁会话 {WebUtility.HtmlEncode(request.SessionId)}";
            return null;
        });
    }

    private static string FormatSession(SandboxSessionInfo info)
    {
        return
            $"SessionId: {WebUtility.HtmlEncode(info.SessionId)}<br/>" +
            $"Template: {WebUtility.HtmlEncode(info.TemplateKey)}<br/>" +
            $"Runtime: {info.RuntimeKind}<br/>" +
            $"Status: {info.Status}<br/>" +
            $"HostPort(loopback): {info.HostPort?.ToString() ?? "-"}<br/>" +
            $"Url(proxy): {WebUtility.HtmlEncode(info.AccessUrl ?? "-")}<br/>" +
            $"Expires(UTC): {info.ExpiresAtUtc:u}<br/>" +
            $"Message: {WebUtility.HtmlEncode(info.StatusMessage ?? "-")}";
    }
}
