/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Constants.cs
    File description: Constants related implementation

    Created by: Senparc - 20231113

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.20.16 Localized PromptRange administration and runtime surfaces

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Senparc.AI;

namespace Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel.Dto
{
    /// <summary>Provides provider names and supported API-version constants for PromptRange.</summary>
    public static class Constants
    {
        // Ordered from legacy-compatible versions to newer preview versions.
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
