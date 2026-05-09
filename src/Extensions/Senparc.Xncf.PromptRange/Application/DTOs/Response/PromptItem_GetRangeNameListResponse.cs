using Senparc.Ncf.Core.AppServices;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.response
{
    public class PromptItem_GetRangeNameListResponse : AppResponseBase<string>
    {
        public int Id { get; set; }

        public string RangeName { get; set; }
    }
}