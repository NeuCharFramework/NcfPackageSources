/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: BuildXncfAppService.AI.MCP.cs
    File description: BuildXncfAppService.AI.MCP related implementation

    Created by: Senparc - 20250524

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260717
    Change description: v0.37.0-preview5 Updated module behavior and related support.

    Modified by: Senparc - 20260804
    Change description: v0.39.0-preview8 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.41.10 Localized XNCF Builder tools and template surfaces

----------------------------------------------------------------*/

using ModelContextProtocol.Server;
using Senparc.Ncf.Core.AppServices;
using Senparc.Xncf.XncfBuilder.OHS.PL;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Senparc.CO2NET.Extensions;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Xncf.XncfBuilder.Domain.Services.Workspace;
namespace Senparc.Xncf.XncfBuilder.OHS.Local
{
    /// <summary>
    /// MCP Server Tools
    /// </summary>
    public partial class BuildXncfAppService
    {
        /// <summary>
        /// Resolves the physical directory for an XNCF module in the default solution
        /// and rejects ambiguous, missing, or unsafe module paths through the workspace service.
        /// </summary>
        /// <param name="moduleName">The complete XNCF project name.</param>
        /// <returns>The resolved module directory.</returns>
        /// <exception cref="Exception">
        /// Propagates validation or filesystem exceptions raised while resolving the module.
        /// </exception>
        private string GetModuleDirectory(string moduleName)
        {
            BuildXncf_BuildRequest request = new BuildXncf_BuildRequest();
            var slnPath = request.GetSlnFilePath();
            return XncfWorkspaceFileService.ResolveModuleDirectory(slnPath, moduleName);
        }


        #region Legacy section

        //Legacy comment translated to English.
        //Legacy comment translated to English.
        /// <summary>
        /// Builds a database-enabled sample XNCF module from the MCP-facing scalar
        /// arguments and delegates to the administrator Builder pipeline.
        /// </summary>
        /// <param name="orgName">The organization prefix for the generated project.</param>
        /// <param name="xncfName">The module name used in the generated project identity.</param>
        /// <param name="version">The module version metadata.</param>
        /// <param name="menuName">The administrator menu display name.</param>
        /// <param name="icon">The administrator menu icon identifier.</param>
        /// <param name="description">The module description metadata.</param>
        /// <returns>A response containing the generated project result or an error.</returns>
        public async Task<StringAppResponse> Build(
            //Legacy comment translated to English.
            // string slnFilePath, 
            [LocalizedDescription(typeof(XncfBuilderResource), "Parameter.XncfBuilder.Organization")]
            string orgName,
            [Required, LocalizedDescription(typeof(XncfBuilderResource), "Parameter.XncfBuilder.ModuleName")]
            string xncfName,
            [Required, LocalizedDescription(typeof(XncfBuilderResource), "Parameter.XncfBuilder.Version")]
            string version,
            [Required, LocalizedDescription(typeof(XncfBuilderResource), "Parameter.XncfBuilder.MenuName")]
            string menuName,
            [Required, LocalizedDescription(typeof(XncfBuilderResource), "Parameter.XncfBuilder.Icon")]
            string icon,
            [LocalizedDescription(typeof(XncfBuilderResource), "Parameter.XncfBuilder.Description")]
            string description)
        {
            Console.WriteLine("XNCF Builder: Receive MCP Call");

            BuildXncf_BuildRequest request = new BuildXncf_BuildRequest()
            {
                //   SlnFilePath = slnFilePath,
                OrgName = orgName,
                XncfName = xncfName,
                Version = version,
                MenuName = menuName,
                Icon = icon,
                Description = description,
                                UseSammple = true,
                                UseModule = new[] { "database" },
                //   UseWeb = new Ncf.XncfBase.Functions.SelectionList( Ncf.XncfBase.Functions.SelectionType.CheckBoxList, new[] {
                //     new Ncf.XncfBase.Functions.SelectionItem("1","useWeb","useWeb",true),
                //   }),
                //   UseWebApi = new Ncf.XncfBase.Functions.SelectionList( Ncf.XncfBase.Functions.SelectionType.CheckBoxList, new[] {
                //     new Ncf.XncfBase.Functions.SelectionItem("1","useWebApi","useWebApi",true),
                //   }),
                                NewSlnFile = new[] { "backup" },
                                TemplatePackage = "no",
                                FrameworkVersion = "net8.0"
            };

            request.SlnFilePath = request.GetSlnFilePath();

            Console.WriteLine("XNCF Builder parameters:" + request.ToJson(true));

            return await this.Build(request);
        }

        #endregion

        /// <summary>
        /// Returns the generated frontend template content.
        /// </summary>
        public Task<string> GetFrontEndCodeTemplate()
        {
            return Task.FromResult(FrontendTemplate);
        }

        /// <summary>
        /// Returns the generated backend template content.
        /// </summary>
        public Task<string> GetBackEndCodeTemplate()
        {
            return Task.FromResult(BackendTemplate);
        }

        /// <summary>
        /// Reads one module file and returns its content and SHA-256 hash.
        /// </summary>
        /// <param name="moduleName">The module name used to resolve its workspace.</param>
        /// <param name="filePath">The module-relative file path.</param>
        /// <returns>A response containing file content or an error message.</returns>
        public async Task<BuildXncf_GetFileResponse> GetFile(
            [LocalizedDescription(typeof(XncfBuilderResource), "XncfBuilder.MCP.ModuleFullName")] string moduleName,
            [LocalizedDescription(typeof(XncfBuilderResource), "XncfBuilder.MCP.FilePath")] string filePath)
        {
            var response = new BuildXncf_GetFileResponse();

            try
            {
                var moduleDirectory = GetModuleDirectory(moduleName);
                var result = await XncfWorkspaceFileService.ReadTextAsync(
                    moduleDirectory,
                    filePath,
                    this.CancellationToken).ConfigureAwait(false);

                response.Success = true;
                response.FileName = Path.GetFileName(result.FullFilePath);
                response.FilePath = filePath;
                response.FileContent = result.Content;
                response.Sha256 = result.Sha256;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
                return response;
            }
            return response;
        }

        /// <summary>
        /// Atomically creates or updates one module file with optional hash concurrency checking.
        /// </summary>
        /// <param name="moduleName">The module name used to resolve its workspace.</param>
        /// <param name="filePath">The module-relative file path.</param>
        /// <param name="fullFileContent">The complete file content to write.</param>
        /// <param name="expectedSha256">Optional previous hash required for a conflict-free update.</param>
        /// <returns>A response containing the new hash and operation result.</returns>
        public async Task<BuildXncf_CreateOrUpdateFileResponse> CreateOrUpdateFile(
           [LocalizedDescription(typeof(XncfBuilderResource), "XncfBuilder.MCP.ModuleFullName")] string moduleName,
           [LocalizedDescription(typeof(XncfBuilderResource), "XncfBuilder.MCP.FilePath")] string filePath,
           [LocalizedDescription(typeof(XncfBuilderResource), "XncfBuilder.MCP.FileContent")] string fullFileContent,
           string expectedSha256 = null)
        {
            var response = new BuildXncf_CreateOrUpdateFileResponse();

            try
            {
                var moduleDirectory = GetModuleDirectory(moduleName);
                var result = await XncfWorkspaceFileService.WriteTextAtomicAsync(
                    moduleDirectory,
                    filePath,
                    fullFileContent,
                    expectedSha256,
                    this.CancellationToken).ConfigureAwait(false);

                response.Success = true;
                response.FileName = Path.GetFileName(result.FullFilePath);
                response.FilePath = filePath;
                response.IsNewFile = result.IsNewFile;
                response.PreviousSha256 = result.PreviousSha256;
                response.Sha256 = result.Sha256;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
            }
            return response;
        }

    }
}
