using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Senparc.AI;

namespace Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel.Dto
{
    /// <summary>
    /// 全局常量配置
    /// </summary>
    public static partial class Constants
    {
        // Azure OpenAI API版本列表
        public static List<string> ApiVersionList = new()
        {
            "2022-12-01", "2023-03-15-preview", "2023-05-15",
            "2023-06-01-preview", "2023-07-01-preview", "2023-08-01-preview"
        };

        public const string OpenAI = "OpenAI";
        public const string AzureOpenAI = "AzureOpenAI";
        public const string HuggingFace = "HuggingFace";


        // public enum ModelTypeEnum
        // {
        //     OpenAI ,
        //     AzureOpenAI,
        //     HuggingFace
        // }
    }
}