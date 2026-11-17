/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: FilePlugin.cs

    File description: Related implementation.

    

    

    Created by: Senparc - 20231003

    

    Modified by: Senparc - 20260702

    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling



    Modified by: Senparc - 20260717

    Change description: v0.37.0-preview5 Updated module behavior and related support.



----------------------------------------------------------------*/

using Microsoft.SemanticKernel;
using Senparc.AI.AgentKernel.Handlers;
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

        [KernelFunction, LocalizedDescription(typeof(XncfBuilderResource), "XncfBuilder.Plugin.CreateEntity")]
        public async Task<FileSaveResult> CreateFile(
             [LocalizedDescription(typeof(XncfBuilderResource), "XncfBuilder.Plugin.FilePath")]
            string fileBasePath,
             [LocalizedDescription(typeof(XncfBuilderResource), "XncfBuilder.Plugin.GeneratedContent")]
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

                var logMsg = XncfBuilderResource.Format("XncfBuilder.Plugin.FileSaved", "已保存文件：{0}", fullPathFileName);
                log.AppendLine(logMsg);

                result.Log += logMsg + "\r\n";
                result.FileContents[fullPathFileName] = fileInfo.EntityCode;
            }

            return result;
        }

        //Legacy comment translated to English.

        [KernelFunction, LocalizedDescription(typeof(XncfBuilderResource), "XncfBuilder.Plugin.ReadDbContext")]
        public async Task<FileSaveResult> UpdateSenparcEntities(
            [LocalizedDescription(typeof(XncfBuilderResource), "XncfBuilder.Plugin.ProjectPath")]
            string projectPath,
            [LocalizedDescription(typeof(XncfBuilderResource), "XncfBuilder.Plugin.EntityName")]
            string entityName,
            [LocalizedDescription(typeof(XncfBuilderResource), "XncfBuilder.Plugin.PluralEntityName")]
            string pluralEntityName
            )
        {
            var result = new FileSaveResult();

            var databaseModelPath = Path.Combine(projectPath, "Domain", "Models", "DatabaseModel");
            var databaseFile = Directory.GetFiles(databaseModelPath, "*SenparcEntities.cs")[0];

            string tempFile = Path.GetTempFileName();

            string targetComment1 = "//DOT REMOVE OR MODIFY THIS LINE"; //"//DOT REMOVE OR MODIFY THIS LINE - Entities Point";
            string targetComment2 = "//DON'T REMOVE OR MODIFY THIS LINE"; //"//DON'T REMOVE OR MODIFY THIS LINE do not remove or modify this line - Entities Point";
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
