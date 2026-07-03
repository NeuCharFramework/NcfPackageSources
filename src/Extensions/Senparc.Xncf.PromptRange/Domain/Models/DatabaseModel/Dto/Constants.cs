/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Constants.cs
    文件功能描述：Constants 相关实现
    
    
    创建标识：Senparc - 20231113
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

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
    public static class Constants
    {
        // Azure OpenAI API版本列表
        public static readonly List<string> ApiVersionList = new()
        {
            "2022-12-01", "2023-03-15-preview", "2023-05-15",
            "2023-06-01-preview", "2023-07-01-preview", "2023-08-01-preview"
        };

        public const string OpenAI = "OpenAI";
        public const string AzureOpenAI = "AzureOpenAI";
        public const string HuggingFace = "HuggingFace";
        public const string NeuCharAI = "NeuCharAI";


        // public enum ModelTypeEnum
        // {
        //     OpenAI ,
        //     AzureOpenAI,
        //     HuggingFace
        // }
    }
}