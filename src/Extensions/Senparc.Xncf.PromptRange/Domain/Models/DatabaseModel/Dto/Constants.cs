/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: Constants.cs

    File description: Constants related implementation

    

    

    Created by: Senparc - 20231113

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



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
    ///Legacy comment translated to English.
    /// </summary>
    public static class Constants
    {
        //Legacy comment translated to English.
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