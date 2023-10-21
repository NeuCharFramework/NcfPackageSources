namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Response
{
    public class LlmModel_GetPageItemResponse
    {
        public int Id { get; set; }

        /// <summary>
        /// 模型名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        public string Developer => "admin";

        public bool Show { get; set; }
    }
}
