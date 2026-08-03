/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：BuildXncfAppService.AI.MCP.cs
    文件功能描述：BuildXncfAppService.AI.MCP 相关实现
    
    
    创建标识：Senparc - 20250524
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

    修改标识：Senparc - 20260717
    修改描述：v0.37.0-preview5 增强 XNCF 构建、数据库迁移与 AI 生成流程的本地化支持

    修改标识：Senparc - 20260804
    修改描述：v0.39.0-preview8 新增 XNCF 隔离预览持久化与跨数据库迁移支持

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
        private string GetModuleDirectory(string moduleName)
        {
            BuildXncf_BuildRequest request = new BuildXncf_BuildRequest();
            var slnPath = request.GetSlnFilePath();
            return XncfWorkspaceFileService.ResolveModuleDirectory(slnPath, moduleName);
        }


        #region MCP AI 接入（由于官方组件 bug，暂时使用平铺参数方式接入）

        //[McpServerTool, Description("生成 XNCF 模块")]
        //[FunctionRender("生成 XNCF", "根据配置条件生成 XNCF", typeof(Register))]
        public async Task<StringAppResponse> Build(
            // [Required,Description("解决方案文件路径")]
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
                //     new Ncf.XncfBase.Functions.SelectionItem("1","使用Web","使用Web",true),
                //   }),
                //   UseWebApi = new Ncf.XncfBase.Functions.SelectionList( Ncf.XncfBase.Functions.SelectionType.CheckBoxList, new[] {
                //     new Ncf.XncfBase.Functions.SelectionItem("1","使用WebApi","使用WebApi",true),
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

        //[McpServerTool, Description("获取前端代码模板示例")]
        public Task<string> GetFrontEndCodeTemplate()
        {
            return Task.FromResult(FrontendTemplate);
        }

        //[McpServerTool, Description("获取后端代码模板示例")]
        public Task<string> GetBackEndCodeTemplate()
        {
            return Task.FromResult(BackendTemplate);
        }

        //[McpServerTool, Description("获取文件内容")]
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

        //[McpServerTool, Description("创建或更新文件内容，文件不存在时会自动创建")]
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
