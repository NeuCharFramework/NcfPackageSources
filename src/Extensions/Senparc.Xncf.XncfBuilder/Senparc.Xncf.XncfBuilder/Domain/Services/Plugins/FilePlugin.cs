using Microsoft.SemanticKernel.SkillDefinition;
using Senparc.AI.Kernel.Handlers;
using Senparc.CO2NET.Helpers;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.XncfBuilder.Domain.Services.Plugins
{
    public class FilePlugin
    {
        private readonly IWantToRun _iWantToRun;

        public FilePlugin(IWantToRun iWantToRun)
        {
            this._iWantToRun = iWantToRun;
        }

        [SKFunction, SKName("CreateFile"), Description("创建实体类")]
        public async Task<string> CreateFile(
             [Description("文件路径")]
            string fileBasePath,
             [Description("通过 AI 生成的文件内容")]
            string fileGenerateResult
         )
        {
            var log = new StringBuilder();
            var result = fileGenerateResult.GetObject<FileGenerateResult[]>();
            foreach (var fileInfo in result)
            {
                var fullPathFileName = Path.GetFullPath(Path.Combine(fileBasePath, fileInfo.FileName));

                CO2NET.Helpers.FileHelper.TryCreateDirectory(Path.GetDirectoryName(fullPathFileName));

                using (var fs = new FileStream(fullPathFileName, FileMode.Create))
                {
                    using (var sw = new StreamWriter(fs))
                    {
                        await sw.WriteAsync(fileInfo.FileContent);
                        await sw.FlushAsync();
                    }
                }

                log.AppendLine($"已保存文件：{fullPathFileName}");
            }

            return log.ToString();
        }

        //TODO：文件修改（从文件中抽取，然后给到 LLM 进行修改）


        [SKFunction, SKName("UpdateSenparcEntities"), Description("读取数据库上下文")]
        public async Task<string> UpdateSenparcEntities(
            [Description("项目路径")]
            string projectPath,
            [Description("新实体的名字")]
            string entityName
            )
        {
            string fileContent = null;

            var databaseModelPath = Path.Combine(projectPath, "Domain", "Models", "DatabaseModel");
            var databaseFile = Directory.GetFiles(databaseModelPath, "*SenparcEntities.cs")[0];
            using (var fs = new FileStream(databaseFile, FileMode.Open))
            {
                using (var sr = new StreamReader(fs))
                {
                    fileContent = sr.ReadToEnd();

                    using (var sw = new StreamWriter(fs))
                    {
                        fs.Seek(0, SeekOrigin.Begin);

                        //运行 plugin
                        //var plugins = new Dictionary<string, List<string>>() {
                        //        {"XncfBuilderPlugin",new(){ "UpdateSenparcEntities" } }
                        //    };

                        var pluginDir = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Domain", "PromptPlugins");
                        var skills = _iWantToRun.ImportSkillFromDirectory(pluginDir, "XncfBuilderPlugin");

                        //运行
                        var request = _iWantToRun.CreateRequest(true, skills.skillList["UpdateSenparcEntities"]);


                        request.TempAiContext = new AI.Kernel.Entities.SenparcAiContext();
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
}
