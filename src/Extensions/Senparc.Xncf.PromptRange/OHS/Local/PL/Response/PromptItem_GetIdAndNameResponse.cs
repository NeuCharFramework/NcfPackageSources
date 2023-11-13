using Senparc.Ncf.Core.AppServices;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.response
{
    public class PromptItem_GetIdAndNameResponse : AppResponseBase<string>
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }
}
