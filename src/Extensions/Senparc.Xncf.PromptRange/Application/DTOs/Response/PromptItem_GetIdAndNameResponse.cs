/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptItem_GetIdAndNameResponse.cs
    File description: PromptItem_GetIdAndNameResponse related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.20.16 Localized PromptRange administration and runtime surfaces

----------------------------------------------------------------*/

using Senparc.Ncf.Core.AppServices;
using Senparc.Xncf.PromptRange.Domain.Models;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.response
{
    public class PromptItem_GetIdAndNameResponse : AppResponseBase<string>
    {
        /// <summary>
        /// primary key ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// display name
        /// </summary>
        public string NickName { get; set; }

        /// <summary>
        /// rangename
        /// </summary>
        public string RangeName { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string FullVersion { get; set; }
        public PromptItemVersion PromptItemVersion { get; set; }

        public PromptItemVersion ParentPromptItemVersion { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public decimal EvalAvgScore { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public decimal EvalMaxScore { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public bool IsDraft { get; set; }

        public string PromptContent { get; set; }

        public PromptItem_GetIdAndNameResponse()
        {
        }

        public PromptItem_GetIdAndNameResponse(PromptItem promptItem)
        {
            Id = promptItem.Id;
            FullVersion = promptItem.FullVersion;
            NickName = promptItem.NickName;
            RangeName = promptItem.RangeName;
            PromptItemVersion = PromptItem.GetVersionObject(promptItem.FullVersion);
            ParentPromptItemVersion = PromptItemVersion with
            {
                Tactic = promptItem.ParentTac,// PromptItem.GetParentTasticFromTastic(PromptItemVersion.Tactic),
                Aim = -1
            };

            EvalAvgScore = promptItem.EvalAvgScore;
            EvalMaxScore = promptItem.EvalMaxScore;
            IsDraft = promptItem.IsDraft;
            PromptContent = promptItem.Content;
        }
    }
}