using Senparc.Ncf.Core.AppServices;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.response
{
    public class PromptItem_GetIdAndNameResponse : AppResponseBase<string>
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string FullVersion { get; set; }

        public int EvalAvgScore { get; set; }

        public int EvalMaxScore { get; set; }

        public bool IsDraft { get; set; }

        public PromptItem_GetIdAndNameResponse()
        {
        }

        public PromptItem_GetIdAndNameResponse(PromptItem promptItem)
        {
            Id = promptItem.Id;
            Name = promptItem.RangeName;
            FullVersion = promptItem.FullVersion;
            EvalAvgScore = promptItem.EvalAvgScore;
            EvalMaxScore = promptItem.EvalMaxScore;
            IsDraft = promptItem.IsDraft;
        }
    }
}