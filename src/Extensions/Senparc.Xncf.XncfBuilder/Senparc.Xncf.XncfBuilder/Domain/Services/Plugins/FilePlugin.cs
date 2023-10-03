using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel.SkillDefinition;
using Senparc.CO2NET.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.XncfBuilder.Domain.Services.Plugins
{
    public class FilePlugin
    {
        [SKFunction, SKName("Create"), Description("创建实体类")]
        public async Task<string> Create(
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


    }
}
