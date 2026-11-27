/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: XncfDevelopmentJobAppService.cs
    File description: Related implementation.

    Created by: Senparc - 20260814

    Modified by: Senparc - 20260815
    Change description: v0.41.0 Updated module behavior and related support.

    Modified by: Senparc - 20260822
    Change description: v0.41.0 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.41.7 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.41.10 Localized XNCF Builder tools and template surfaces

    Modified by: Senparc - 20261127
    Change description: v0.42.0.0 add MCP endpoint gateway and tool invocation contracts

----------------------------------------------------------------*/

using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.MultiTenant;
using Senparc.Ncf.Core.WorkContext.Provider;
using Senparc.Ncf.Service;
using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Xncf.XncfBuilder.Domain.Services.Development;
using Senparc.Xncf.XncfBuilder;
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

        /// <summary>
        /// Initializes the application-service facade for isolated development jobs.
        /// Administrator identity is resolved from the current admin work context
        /// when a job is created.
        /// </summary>
        /// <param name="serviceProvider">The dependency provider used by the base service.</param>
        /// <param name="adminWorkContextProvider">The provider for the current administrator context.</param>
        public XncfDevelopmentJobAppService(
            IServiceProvider serviceProvider,
            IAdminWorkContextProvider adminWorkContextProvider) : base(serviceProvider)
        {
            _adminWorkContextProvider = adminWorkContextProvider;
        }

        /// <summary>
        /// Creates an isolated development job whose source snapshot is detached
        /// from the target checkout.
        /// </summary>
        /// <param name="request">The solution, module, generation mode, and template options.</param>
        /// <returns>A serialized response containing the created job or an error.</returns>
        [FunctionRender(typeof(XncfBuilderResource), "Function.XncfBuilder.Development.Create.Name", "Function.XncfBuilder.Development.Create.Description",
            typeof(Register), "Create isolated XNCF development job", "Create a source snapshot in an isolated workspace. Target source is never written.")]
        public Task<StringAppResponse> CreateIsolatedDevelopmentJob(XncfDevelopmentStartRequest request) =>
            ExecuteAsync(async (service, tenant) =>
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
                }, CancellationToken,
                tenant).ConfigureAwait(false);
                return result;
            });

        /// <summary>Reads an isolated development file and returns its content and hash.</summary>
        /// <param name="request">The job identifier and relative file path.</param>
        [FunctionRender(typeof(XncfBuilderResource), "Function.XncfBuilder.Development.Read.Name", "Function.XncfBuilder.Development.Read.Description",
            typeof(Register), "Read isolated XNCF file", "Read a text file and SHA-256 from an isolated workspace module.")]
        public Task<StringAppResponse> ReadIsolatedDevelopmentFile(XncfDevelopmentReadFileRequest request) =>
            ExecuteAsync((service, tenant) => service.ReadFileAsync(request.JobId, request.RelativeFilePath, CancellationToken, tenant));

        /// <summary>Atomically writes one isolated development file with optional hash concurrency checking.</summary>
        /// <param name="request">The job identifier, relative path, content, and expected hash.</param>
        [FunctionRender(typeof(XncfBuilderResource), "Function.XncfBuilder.Development.Write.Name", "Function.XncfBuilder.Development.Write.Description",
            typeof(Register), "Write isolated XNCF file", "Atomically write a code file inside the isolated workspace.")]
        public Task<StringAppResponse> WriteIsolatedDevelopmentFile(XncfDevelopmentWriteFileRequest request) =>
            ExecuteAsync((service, tenant) => service.WriteFileAsync(
                request.JobId,
                request.RelativeFilePath,
                request.Content,
                request.ExpectedSha256,
                CancellationToken,
                tenant));

        /// <summary>Validates the isolated development job and its direct source-project reference.</summary>
        /// <param name="request">The development job identifier.</param>
        [FunctionRender(typeof(XncfBuilderResource), "Function.XncfBuilder.Development.Validate.Name", "Function.XncfBuilder.Development.Validate.Description",
            typeof(Register), "Validate isolated XNCF development job", "Validate the isolated module path and direct source-project reference.")]
        public Task<StringAppResponse> ValidateIsolatedDevelopmentJob(XncfDevelopmentJobIdRequest request) =>
            ExecuteAsync((service, tenant) => service.ValidateAsync(request.JobId, CancellationToken, tenant));

        /// <summary>Builds and starts the isolated development preview through Sandbox.</summary>
        /// <param name="request">The development job identifier.</param>
        [FunctionRender(typeof(XncfBuilderResource), "Function.XncfBuilder.Development.StartPreview.Name", "Function.XncfBuilder.Development.StartPreview.Description",
            typeof(Register), "Start Sandbox XNCF preview", "Build and run the cleaned isolated workspace through Sandbox.")]
        public Task<StringAppResponse> StartSandboxDevelopmentPreview(XncfDevelopmentJobIdRequest request) =>
            ExecuteAsync((service, tenant) => service.StartSandboxPreviewAsync(request.JobId, CancellationToken, tenant));

        /// <summary>Returns isolated workspace, validation, preview, diff, and approval status.</summary>
        /// <param name="request">The development job identifier.</param>
        [FunctionRender(typeof(XncfBuilderResource), "Function.XncfBuilder.Development.Status.Name", "Function.XncfBuilder.Development.Status.Description",
            typeof(Register), "Get isolated XNCF development job", "Return isolated workspace, diff, validation, preview, and approval status.")]
        public Task<StringAppResponse> GetIsolatedDevelopmentJob(XncfDevelopmentJobIdRequest request) =>
            ExecuteAsync((service, tenant) => service.GetAsync(request.JobId, CancellationToken, tenant));

        /// <summary>Freezes the isolated diff and requests administrator merge approval.</summary>
        /// <param name="request">The development job identifier.</param>
        [FunctionRender(typeof(XncfBuilderResource), "Function.XncfBuilder.Development.RequestMerge.Name", "Function.XncfBuilder.Development.RequestMerge.Description",
            typeof(Register), "Request human merge approval", "Freeze the isolated workspace diff and request administrator approval.")]
        public Task<StringAppResponse> RequestIsolatedDevelopmentMergeApproval(XncfDevelopmentJobIdRequest request) =>
            ExecuteAsync((service, tenant) => service.RequestMergeApprovalAsync(request.JobId, CancellationToken, tenant));

        /// <summary>Discards the isolated development job and reclaims its preview resources.</summary>
        /// <param name="request">The development job identifier.</param>
        [FunctionRender(typeof(XncfBuilderResource), "Function.XncfBuilder.Development.Discard.Name", "Function.XncfBuilder.Development.Discard.Description",
            typeof(Register), "Discard isolated XNCF development job", "Stop the related Sandbox preview and reclaim the isolated workspace.")]
        public Task<StringAppResponse> DiscardIsolatedDevelopmentJob(XncfDevelopmentJobIdRequest request) =>
            ExecuteAsync((service, tenant) => service.DiscardAsync(request.JobId, CancellationToken, tenant));

        /// <summary>
        /// Executes one development-job operation inside the standard response/error pipeline.
        /// </summary>
        /// <typeparam name="T">The operation result type.</typeparam>
        /// <param name="operation">The service operation to invoke.</param>
        /// <returns>A string response containing serialized result or error state.</returns>
        private async Task<StringAppResponse> ExecuteAsync<T>(Func<IXncfDevelopmentJobService, RequestTenantInfo?, Task<T>> operation)
        {
            return await this.GetStringResponseAsync(async (response, logger) =>
            {
                try
                {
                    // Preserve the ambient tenant while invoking the isolated job service.
                    var ambientTenant = ServiceProvider.GetService<RequestTenantInfo>();
                    var service = ServiceProvider.GetRequiredService<IXncfDevelopmentJobService>();
                    var result = await operation(service, ambientTenant).ConfigureAwait(false);
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
