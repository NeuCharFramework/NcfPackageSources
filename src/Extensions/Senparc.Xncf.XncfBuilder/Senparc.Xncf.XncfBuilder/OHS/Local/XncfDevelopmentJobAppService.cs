/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：XncfDevelopmentJobAppService.cs
    文件功能描述：供 Admin Chat 调用的受控 XNCF 开发工作流 Function

    创建标识：Senparc - 20260814

    修改标识：Senparc - 20260815
    修改描述：v0.41.0-preview11 增强隔离开发任务与 Sandbox 预览流程

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

        [FunctionRender(typeof(XncfBuilderResource), "Function.XncfBuilder.Development.Create.Name", "Function.XncfBuilder.Development.Create.Description", typeof(Register))]
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

        [FunctionRender(typeof(XncfBuilderResource), "Function.XncfBuilder.Development.Read.Name", "Function.XncfBuilder.Development.Read.Description", typeof(Register))]
        public Task<StringAppResponse> ReadIsolatedDevelopmentFile(XncfDevelopmentReadFileRequest request) =>
            ExecuteAsync(service => service.ReadFileAsync(request.JobId, request.RelativeFilePath, CancellationToken));

        [FunctionRender(typeof(XncfBuilderResource), "Function.XncfBuilder.Development.Write.Name", "Function.XncfBuilder.Development.Write.Description", typeof(Register))]
        public Task<StringAppResponse> WriteIsolatedDevelopmentFile(XncfDevelopmentWriteFileRequest request) =>
            ExecuteAsync(service => service.WriteFileAsync(
                request.JobId,
                request.RelativeFilePath,
                request.Content,
                request.ExpectedSha256,
                CancellationToken));

        [FunctionRender(typeof(XncfBuilderResource), "Function.XncfBuilder.Development.Validate.Name", "Function.XncfBuilder.Development.Validate.Description", typeof(Register))]
        public Task<StringAppResponse> ValidateIsolatedDevelopmentJob(XncfDevelopmentJobIdRequest request) =>
            ExecuteAsync(service => service.ValidateAsync(request.JobId, CancellationToken));

        [FunctionRender(typeof(XncfBuilderResource), "Function.XncfBuilder.Development.Preview.Name", "Function.XncfBuilder.Development.Preview.Description", typeof(Register))]
        public Task<StringAppResponse> StartSandboxDevelopmentPreview(XncfDevelopmentJobIdRequest request) =>
            ExecuteAsync(service => service.StartSandboxPreviewAsync(request.JobId, cancellationToken: CancellationToken));

        [FunctionRender(typeof(XncfBuilderResource), "Function.XncfBuilder.Development.Status.Name", "Function.XncfBuilder.Development.Status.Description", typeof(Register))]
        public Task<StringAppResponse> GetIsolatedDevelopmentJob(XncfDevelopmentJobIdRequest request) =>
            ExecuteAsync(service => service.GetAsync(request.JobId, CancellationToken));

        [FunctionRender(typeof(XncfBuilderResource), "Function.XncfBuilder.Development.RequestApproval.Name", "Function.XncfBuilder.Development.RequestApproval.Description", typeof(Register))]
        public Task<StringAppResponse> RequestIsolatedDevelopmentMergeApproval(XncfDevelopmentJobIdRequest request) =>
            ExecuteAsync(service => service.RequestMergeApprovalAsync(request.JobId, CancellationToken));

        [FunctionRender(typeof(XncfBuilderResource), "Function.XncfBuilder.Development.Discard.Name", "Function.XncfBuilder.Development.Discard.Description", typeof(Register))]
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
