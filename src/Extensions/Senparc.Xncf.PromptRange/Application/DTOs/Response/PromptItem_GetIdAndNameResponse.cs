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
        /// Nick name
        /// </summary>
        public string NickName { get; set; }

        /// <summary>
        ///shooting range name
        /// </summary>
        public string RangeName { get; set; }

        /// <summary>
        ///Full version number
        /// </summary>
        public string FullVersion { get; set; }
        public PromptItemVersion PromptItemVersion { get; set; }

        public PromptItemVersion ParentPromptItemVersion { get; set; }

        /// <summary>
        /// evaluation parameters, average score
        /// </summary>
        public decimal EvalAvgScore { get; set; }

        /// <summary>
        ///evaluation parameters
        /// </summary>
        public decimal EvalMaxScore { get; set; }

        /// <summary>
        /// Is it a draft?
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