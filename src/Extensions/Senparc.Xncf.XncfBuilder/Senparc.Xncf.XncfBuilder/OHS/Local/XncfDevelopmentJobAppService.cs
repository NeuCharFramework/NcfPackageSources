/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：XncfDevelopmentJobAppService.cs
    文件功能描述：供 Admin Chat 调用的受控 XNCF 开发工作流 Function

    创建标识：Senparc - 20260814

----------------------------------------------------------------*/

using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.WorkContext.Provider;
using Senparc.Ncf.Service;
using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Xncf.XncfBuilder.Domain.Services.Development;
using Senparc.Xncf.XncfBuilder.OHS.PL;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace Senparc.Xncf.XncfBuilder.OHS.Local
{
    /// <summary>
    /// These functions deliberately expose only isolated workspaces. In particular there is no
    /// FunctionRender for applying a job: chat may request approval, but a human must approve it
    /// through the antiforgery-protected Admin page.
    /// </summary>
    public sealed class XncfDevelopmentJobAppService : AppServiceBase
    {
        private readonly IAdminWorkContextProvider _adminWorkContextProvider;

        public XncfDevelopmentJobAppService(
            IServiceProvider serviceProvider,
            IAdminWorkContextProvider adminWorkContextProvider) : base(serviceProvider)
        {
            _adminWorkContextProvider = adminWorkContextProvider;
        }

        [FunctionRender("创建隔离 XNCF 开发任务", "创建源码快照；新模块仅在隔离工作区由本机已安装模板生成，现有模块仅复制后修改。不会写入目标源码。", typeof(Register))]
        public Task<StringAppResponse> CreateIsolatedDevelopmentJob(XncfDevelopmentStartRequest request) =>
            ExecuteAsync(async service =>
            {
                var result = await service.CreateAsync(new XncfDevelopmentCreateOptions
                {
                    OwnerAdminUserId = _adminWorkContextProvider.GetAdminWorkContext().AdminUserId,
                    SolutionFilePath = request.SlnFilePath,
                    Mode = request.Mode,
                    ModuleProjectName = request.ModuleProjectName,
                    OrganizationName = request.OrgName,
                    XncfName = request.XncfName,
                    TargetFramework = request.TargetFramework,
                    Version = request.Version,
                    MenuName = request.MenuName,
                    Icon = request.Icon,
                    Description = request.Description,
                    Requirement = request.Requirement,
                    IncludeFunction = request.IncludeFunction,
                    IncludeDatabase = request.IncludeDatabase,
                    IncludeWeb = request.IncludeWeb,
                    IncludeWebApi = request.IncludeWebApi,
                    IncludeSample = request.IncludeSample
                }, CancellationToken).ConfigureAwait(false);
                return result;
            });

        [FunctionRender("读取隔离 XNCF 文件", "读取隔离工作区中指定模块的文本文件和 SHA-256；不能读取目标源码或工作区外的文件。", typeof(Register))]
        public Task<StringAppResponse> ReadIsolatedDevelopmentFile(XncfDevelopmentReadFileRequest request) =>
            ExecuteAsync(service => service.ReadFileAsync(request.JobId, request.RelativeFilePath, CancellationToken));

        [FunctionRender("写入隔离 XNCF 文件", "原子写入隔离工作区内的代码文件。项目、NuGet、MSBuild 和应用配置文件不可修改，建议传回读取时的 SHA-256。", typeof(Register))]
        public Task<StringAppResponse> WriteIsolatedDevelopmentFile(XncfDevelopmentWriteFileRequest request) =>
            ExecuteAsync(service => service.WriteFileAsync(
                request.JobId,
                request.RelativeFilePath,
                request.Content,
                request.ExpectedSha256,
                CancellationToken));

        [FunctionRender("校验隔离 XNCF 开发任务", "校验隔离工作区的模块路径和 Senparc.Web 对源码项目的直接引用，并生成受控差异摘要。", typeof(Register))]
        public Task<StringAppResponse> ValidateIsolatedDevelopmentJob(XncfDevelopmentJobIdRequest request) =>
            ExecuteAsync(service => service.ValidateAsync(request.JobId, CancellationToken));

        [FunctionRender("启动 Sandbox XNCF 预览", "把已清洗的隔离工作区副本交给 Sandbox 构建和运行；Sandbox 不可用时会安全失败，绝不回退到主站进程。", typeof(Register))]
        public Task<StringAppResponse> StartSandboxDevelopmentPreview(XncfDevelopmentJobIdRequest request) =>
            ExecuteAsync(service => service.StartSandboxPreviewAsync(request.JobId, cancellationToken: CancellationToken));

        [FunctionRender("获取隔离 XNCF 开发任务状态", "返回隔离工作区、差异、校验、Sandbox 预览和人工审批状态。", typeof(Register))]
        public Task<StringAppResponse> GetIsolatedDevelopmentJob(XncfDevelopmentJobIdRequest request) =>
            ExecuteAsync(service => service.GetAsync(request.JobId, CancellationToken));

        [FunctionRender("请求人工合入 XNCF 任务", "冻结当前隔离工作区差异并请求管理员审批。此操作不会写入目标源码。", typeof(Register))]
        public Task<StringAppResponse> RequestIsolatedDevelopmentMergeApproval(XncfDevelopmentJobIdRequest request) =>
            ExecuteAsync(service => service.RequestMergeApprovalAsync(request.JobId, CancellationToken));

        [FunctionRender("丢弃隔离 XNCF 开发任务", "停止对应 Sandbox 预览并回收隔离工作区；不会修改目标源码。", typeof(Register))]
        public Task<StringAppResponse> DiscardIsolatedDevelopmentJob(XncfDevelopmentJobIdRequest request) =>
            ExecuteAsync(service => service.DiscardAsync(request.JobId, CancellationToken));

        private async Task<StringAppResponse> ExecuteAsync<T>(Func<IXncfDevelopmentJobService, Task<T>> operation)
        {
            return await this.GetStringResponseAsync(async (response, logger) =>
            {
                try
                {
                    var service = ServiceProvider.GetRequiredService<IXncfDevelopmentJobService>();
                    var result = await operation(service).ConfigureAwait(false);
                    response.Success = true;
                    response.Data = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
                }
                catch (Exception ex)
                {
                    response.Success = false;
                    response.StateCode = 101;
                    response.ErrorMessage = ex.Message;
                    response.Data = ex.Message;
                    logger.Append($"XNCF isolated development workflow failed: {ex.Message}");
                }
                return null;
            }).ConfigureAwait(false);
        }
    }
}
