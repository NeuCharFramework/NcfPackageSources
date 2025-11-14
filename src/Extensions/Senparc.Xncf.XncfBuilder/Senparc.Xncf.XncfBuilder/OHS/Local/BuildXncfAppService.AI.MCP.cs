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
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Senparc.AI.Entities.Keys;

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
                throw new Exception($"未找到模块 {moduleName} 的目录，请检查模块名称是否完整，必须完全匹配，如：Senparc.Xncf.XncfBuilder。");
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
            [Description("组织名称，默认为 Senparc")]
            string orgName,
            [Required, Description("模块名称")]
            string xncfName,
            [Required, Description("版本号，默认为 1.0.0")]
            string version,
            [Required, Description("菜单显示名称")]
            string menuName,
            [Required, Description("图标，支持 Font Awesome 图标集")]
            string icon,
            [Description("模块说明")]
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
                UseSammple = new Ncf.XncfBase.Functions.SelectionList(Ncf.XncfBase.Functions.SelectionType.CheckBoxList, new[] {
                new Ncf.XncfBase.Functions.SelectionItem("1","使用示例","使用示例",true),
              }),
                UseModule = new Ncf.XncfBase.Functions.SelectionList(Ncf.XncfBase.Functions.SelectionType.CheckBoxList, new[] {
                new Ncf.XncfBase.Functions.SelectionItem("database","数据库","使用数据库",true),
              }),
                //   UseWeb = new Ncf.XncfBase.Functions.SelectionList( Ncf.XncfBase.Functions.SelectionType.CheckBoxList, new[] {
                //     new Ncf.XncfBase.Functions.SelectionItem("1","使用Web","使用Web",true),
                //   }),
                //   UseWebApi = new Ncf.XncfBase.Functions.SelectionList( Ncf.XncfBase.Functions.SelectionType.CheckBoxList, new[] {
                //     new Ncf.XncfBase.Functions.SelectionItem("1","使用WebApi","使用WebApi",true),
                //   }),
                NewSlnFile = new Ncf.XncfBase.Functions.SelectionList(Ncf.XncfBase.Functions.SelectionType.CheckBoxList, new[] {
                new Ncf.XncfBase.Functions.SelectionItem("backup","备份 .sln 文件（推荐）","如果使用覆盖现有 .sln 文件，对当前文件进行备份",true),
              }),
                TemplatePackage = new Ncf.XncfBase.Functions.SelectionList(Ncf.XncfBase.Functions.SelectionType.DropDownList, new[] {
                new Ncf.XncfBase.Functions.SelectionItem("no","已安装，不需要安装新版本","请确保已经在本地安装过版本（无论新旧），否则将自动从在线获取",true),
              }),
                FrameworkVersion = new Ncf.XncfBase.Functions.SelectionList(Ncf.XncfBase.Functions.SelectionType.DropDownList, new[] {
                new Ncf.XncfBase.Functions.SelectionItem("net8.0","net8.0","使用 .NET 8.0",false),
              })
            };

            request.SlnFilePath = request.GetSlnFilePath();
            request.UseSammple.SelectedValues = new[] { "1" };
            request.UseModule.SelectedValues = new[] { "database" };
            request.NewSlnFile.SelectedValues = new[] { "backup" };
            request.TemplatePackage.SelectedValues = new[] { "no" };
            request.FrameworkVersion.SelectedValues = new[] { "net8.0" };

            Console.WriteLine("XNCF Builder parameters:" + request.ToJson(true));

            return await this.Build(request);
        }

        #endregion

        //[McpServerTool, Description("获取前端代码模板示例")]
        public async Task<string> GetFrontEndCodeTemplate()
        {
            var template = BuildXncfAppService.FrontendTemplate;
            return template;

        }

        //[McpServerTool, Description("获取后端代码模板示例")]
        public async Task<string> GetBackEndCodeTemplate()
        {
            var template = BuildXncfAppService.BackendTemplate;

            return template;
        }

        //[McpServerTool, Description("获取文件内容")]
        public async Task<BuildXncf_GetFileResponse> GetFile([Description("完整模块名，如 Senparc.Xncf.XncfBuilder")] string moduleName, [Description("在模块内的路径+文件名")] string filePath)
        {
            var response = new BuildXncf_GetFileResponse();

            string fullFilePath = null;
            string fileContent = null;
            try
            {
                fullFilePath = this.GetFilePath(moduleName, filePath);

                if (!File.Exists(fullFilePath))
                {
                    throw new Exception("文件不存在：" + filePath);
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
        public async Task<BuildXncf_CreateOrUpdateFileResponse> CreateOrUpdateFile([Description("完整模块名，如 Senparc.Xncf.XncfBuilder")] string moduleName,
           [Description("在模块内的路径+文件名")] string filePath,
           [Description("完整文件内容")] string fullFileContent)
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
