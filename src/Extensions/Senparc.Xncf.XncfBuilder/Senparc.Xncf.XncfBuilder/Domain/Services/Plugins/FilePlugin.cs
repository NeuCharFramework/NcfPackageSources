using Microsoft.SemanticKernel;
using Senparc.AI.Kernel.Handlers;
using Senparc.CO2NET.Helpers;
using System;
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

        [KernelFunction, Description("创建实体类")]
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

        [KernelFunction, Description("读取数据库上下文")]
        public async Task<FileSaveResult> UpdateSenparcEntities(
            [Description("项目路径")]
            string projectPath,
            [Description("新实体的名字")]
            string entityName,
            [Description("新实体的名字的复数")]
            string pluralEntityName
            )
        {
            var result = new FileSaveResult();

            var databaseModelPath = Path.Combine(projectPath, "Domain", "Models", "DatabaseModel");
            var databaseFile = Directory.GetFiles(databaseModelPath, "*SenparcEntities.cs")[0];

            string tempFile = Path.GetTempFileName();

            string targetComment1 = "//DOT REMOVE OR MODIFY THIS LINE"; //"//DOT REMOVE OR MODIFY THIS LINE 请勿移除或修改本行 - Entities Point";
            string targetComment2 = "//DON'T REMOVE OR MODIFY THIS LINE"; //"//DON'T REMOVE OR MODIFY THIS LINE 请勿移除或修改本行 - Entities Point";
            string insertStr = $"        public DbSet<{entityName}> {pluralEntityName} {{ get; set; }}";
            bool inserted = false;

            using (StreamReader reader = new StreamReader(databaseFile))
            {
                using (StreamWriter writer = new StreamWriter(tempFile))
                {
                    string line;
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        if (!inserted && (line.Contains(targetComment1) || line.Contains(targetComment2)))
                        {
                            await writer.WriteLineAsync(insertStr);
                            await writer.WriteLineAsync("");
                            inserted = true;
                        }
                        await writer.WriteLineAsync(line);
                    }
                }
            }

            if (inserted)
            {
                File.Delete(databaseFile);
                File.Move(tempFile, databaseFile);
                Console.WriteLine("插入成功！");
            }
            else
            {
                File.Delete(tempFile);
                Console.WriteLine("目标注释未找到，未插入内容。");
            }

            result.Log += $"已更新文件：{databaseFile}";
            result.FileContents[databaseFile] = await File.ReadAllTextAsync(databaseFile);

            return result;

        }
    }
}
