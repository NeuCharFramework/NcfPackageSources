/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: LlModelDto.cs
    File description: LlModelDto related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using Senparc.AI;
using Senparc.Ncf.Core.Models;

namespace Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto
{
    public class LlModelDto : DtoBase
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int Id { get; set; }

        public string Alias { get; set; }

        /// <summary>
        /// name（must）
        /// </summary>
        public string DeploymentName { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public AiPlatform ModelType { get; set; }

        /// <summary>
        /// Endpoint（must）
        /// </summary>
        public string Endpoint { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string OrganizationId { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string ApiVersion { get; set; }

        /// <summary>
        /// Note (optional)
        /// </summary>
        public string Note { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int MaxToken { get; set; }


        /// <summary>
        /// whether shared
        /// </summary>
        public bool IsShared { get; set; }

        /// <summary>
        /// whether displayed
        /// </summary>
        public bool Show { get; set; }

        // /// <summary>
        //Legacy comment translated to English.
        // /// </summary>
        // public string TextCompletionModelName { get;  set; }
        //
        // /// <summary>
        //Legacy comment translated to English.
        // /// </summary>
        // public string TextEmbeddingModelName { get;  set; }
        //
        // /// <summary>
        //Legacy comment translated to English.
        // /// </summary>
        // public string OtherModelName { get;  set; }

        private LlModelDto()
        {
        }
    }
}