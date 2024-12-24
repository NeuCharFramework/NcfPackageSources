using Senparc.Ncf.Core.AppServices;
using Senparc.Xncf.PromptRange.Domain.Models;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.response
{
    public class PromptItem_GetIdAndNameResponse : AppResponseBase<string>
    {
        /// <summary>
        /// 主键 ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 昵称
        /// </summary>
        public string NickName { get; set; }

        /// <summary>
        /// 靶场名称
        /// </summary>
        public string RangeName { get; set; }

        /// <summary>
        /// 完整版号
        /// </summary>
        public string FullVersion { get; set; }
        public PromptItemVersion PromptItemVersion { get; set; }

        public PromptItemVersion ParentPromptItemVersion { get; set; }

        /// <summary>
        /// 评估参数, 平均分
        /// </summary>
        public decimal EvalAvgScore { get; set; }

        /// <summary>
        /// 评估参数
        /// </summary>
        public decimal EvalMaxScore { get; set; }

        /// <summary>
        /// 是否是草稿
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