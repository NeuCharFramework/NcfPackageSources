/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：BuildXncfAppService.AI.MCP.cs
    文件功能描述：BuildXncfAppService.AI.MCP 相关实现
    
    
    创建标识：Senparc - 20250524
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

    修改标识：Senparc - 20260717
    修改描述：v0.37.0-preview5 增强 XNCF 构建、数据库迁移与 AI 生成流程的本地化支持

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
namespace Senparc.Xncf.XncfBuilder.OHS.Local
{
    /// <summary>
    /// MCP Server Tools
    /// </summary>
    public partial class BuildXncfAppService
    {
        private string GetFilePath(string moduleName, string filePath)
        {
            BuildXncf_BuildRequest request = new BuildXncf_BuildRequest();
            var slnPath = request.GetSlnFilePath();
            var modulePath = Directory.GetDirectories(Path.GetDirectoryName(slnPath), moduleName, SearchOption.AllDirectories)
                                    .FirstOrDefault();

            if (string.IsNullOrEmpty(modulePath))
            {
                throw new Exception(XncfBuilderResource.Format("XncfBuilder.MCP.ModuleDirectoryNotFound", "未找到模块 {0} 的目录；模块名称必须完整匹配，例如 Senparc.Xncf.XncfBuilder。", moduleName));
            }

            var fullFilePath = Path.Combine(modulePath, filePath);
            return fullFilePath;
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

            string fullFilePath = null;
            string fileContent = null;
            try
            {
                fullFilePath = this.GetFilePath(moduleName, filePath);

                if (!File.Exists(fullFilePath))
                {
                    throw new Exception(XncfBuilderResource.Format("XncfBuilder.MCP.FileNotFound", "文件不存在：{0}", filePath));
                }

                fileContent = await File.ReadAllTextAsync(fullFilePath);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
                return response;
            }

            response.Success = true;
            response.FileName = Path.GetFileName(fullFilePath);
            response.FilePath = filePath;
            response.FileContent = fileContent;
            return response;
        }

        //[McpServerTool, Description("创建或更新文件内容，文件不存在时会自动创建")]
        public async Task<BuildXncf_CreateOrUpdateFileResponse> CreateOrUpdateFile(
           [LocalizedDescription(typeof(XncfBuilderResource), "XncfBuilder.MCP.ModuleFullName")] string moduleName,
           [LocalizedDescription(typeof(XncfBuilderResource), "XncfBuilder.MCP.FilePath")] string filePath,
           [LocalizedDescription(typeof(XncfBuilderResource), "XncfBuilder.MCP.FileContent")] string fullFileContent)
        {
            var response = new BuildXncf_CreateOrUpdateFileResponse();

            string fullFilePath = null;
            string fileContent = fullFileContent;
            try
            {
                fullFilePath = this.GetFilePath(moduleName, filePath);

                if (!File.Exists(fullFilePath))
                {
                    string directoryPath = Path.GetDirectoryName(fullFilePath);
                    Senparc.CO2NET.Helpers.FileHelper.TryCreateDirectory(directoryPath);
                    response.IsNewFile = true;
                }

                //TODO: 使用 SHA1 验证指纹，把旧文件内容进行缓存或差量备份
                await File.WriteAllTextAsync(fullFilePath, fileContent);
                response.Success = true;
                response.FileName = Path.GetFileName(fullFilePath);
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
