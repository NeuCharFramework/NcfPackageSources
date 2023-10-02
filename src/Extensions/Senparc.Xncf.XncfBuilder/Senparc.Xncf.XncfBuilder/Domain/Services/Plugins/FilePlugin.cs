using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel.SkillDefinition;
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
        [SKFunction, SKName("BuildEntityClass"), Description("创建实体类")]
        public async Task<string> CreateAsync(
         [Description("文件名，包含路径")]
        string fullPathFileName,
         [Description("文件内容")]
        string fileContnet
         )
        {
            using (var fs = new FileStream(fullPathFileName, FileMode.Create))
            {
                using (var sw = new StreamWriter(fs))
                {
                    await sw.WriteAsync(fileContnet);
                    await sw.FlushAsync();
                }
                await fs.FlushAsync();
            }

            return "已保存：" + fullPathFileName;
        }

        //TODO：文件修改（从文件中抽取，然后给到 LLM 进行修改）


    }
}
