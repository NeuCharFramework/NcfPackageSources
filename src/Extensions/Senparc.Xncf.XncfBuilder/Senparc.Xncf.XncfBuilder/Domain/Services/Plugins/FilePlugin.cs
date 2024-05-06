using Microsoft.SemanticKernel;
using Senparc.AI.Kernel.Handlers;
using Senparc.CO2NET.Helpers;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.XncfBuilder.Domain.Services.Plugins
{
    public class FilePlugin
    {
        public class FileSaveResult
        {
            public Dictionary<string, string> FileContents { get; set; } = new Dictionary<string, string>();
            public string Log { get; set; }
        }

        private readonly IWantToRun _iWantToRun;

        public FilePlugin(IWantToRun iWantToRun)
        {
            this._iWantToRun = iWantToRun;
        }

        [KernelFunction("CreateFile"), Description("创建实体类")]
        public async Task<FileSaveResult> CreateFile(
             [Description("文件路径")]
            string fileBasePath,
             [Description("通过 AI 生成的文件内容")]
            string fileGenerateResult
         )
        {
            var result = new FileSaveResult();
            var log = new StringBuilder();
            var renerateResult = fileGenerateResult.GetObject<FileGenerateResult[]>();
            var filePaths = new List<string>();
            foreach (var fileInfo in renerateResult)
            {
                var fullPathFileName = Path.GetFullPath(Path.Combine(fileBasePath, fileInfo.FileName));

                CO2NET.Helpers.FileHelper.TryCreateDirectory(Path.GetDirectoryName(fullPathFileName));

                using (var fs = new FileStream(fullPathFileName, FileMode.Create))
                {
                    using (var sw = new StreamWriter(fs))
                    {
                        await sw.WriteAsync(fileInfo.EntityCode);
                        await sw.FlushAsync();
                    }
                }

                var logMsg = $"已保存文件：{fullPathFileName}";
                log.AppendLine(logMsg);

                result.Log += logMsg + "\r\n";
                result.FileContents[fullPathFileName] = fileInfo.EntityCode;
            }

            return result;
        }

        //TODO：文件修改（从文件中抽取，然后给到 LLM 进行修改）

        [KernelFunction("UpdateSenparcEntities"), Description("读取数据库上下文")]
        public async Task<string> UpdateSenparcEntities(
            [Description("项目路径")]
            string projectPath,
            [Description("新实体的名字")]
            string entityName
            )
        {

            var databaseModelPath = Path.Combine(projectPath, "Domain", "Models", "DatabaseModel");
            var databaseFile = Directory.GetFiles(databaseModelPath, "*SenparcEntities.cs")[0];

            string fileContent = await File.ReadAllTextAsync(databaseFile);

            using (var fs = new FileStream(databaseFile, FileMode.Open))
            {
                using (var sw = new StreamWriter(fs))
                {
                    //运行 plugin
                    //var plugins = new Dictionary<string, List<string>>() {
                    //        {"XncfBuilderPlugin",new(){ "UpdateSenparcEntities" } }
                    //    };

                    var pluginDir = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Domain", "PromptPlugins");
                    //var finalDir = Path.Combine(pluginDir, "UpdateSenparcEntities");
                    var skills = _iWantToRun.ImportPluginFromPromptDirectory(pluginDir, "XncfBuilderPlugin");

                    //运行
                    var request = _iWantToRun.CreateRequest(true, skills.kernelPlugin["UpdateSenparcEntities"]);

                    request.TempAiArguments = new AI.Kernel.Entities.SenparcAiArguments();
                    request.SetTempContext("Code", fileContent);
                    request.SetTempContext("EntityName", entityName);

                    var result = await _iWantToRun.RunAsync(request);

                    var newFileContent = result.Output;

                    await sw.WriteAsync(newFileContent);
                    await sw.FlushAsync();

                    return $"已更新文件：{databaseFile}";
                }
            }

        }


    }
}
