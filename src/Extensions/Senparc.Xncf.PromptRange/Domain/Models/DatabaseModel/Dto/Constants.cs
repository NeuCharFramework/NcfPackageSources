using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel.Dto
{
    public class Constants
    {
        // Azure OpenAI API版本列表
        public static List<string> ApiVersionList = new() {
                "2022-12-01", "2023-03-15-preview", "2023-05-15",
                "2023-06-01-preview", "2023-07-01-preview", "2023-08-01-preview"
            };

        public enum ModelTypeEnum
        {
            OpenAI ,
            AzureOpenAI,
            HuggingFace
        }
    }

    
}
