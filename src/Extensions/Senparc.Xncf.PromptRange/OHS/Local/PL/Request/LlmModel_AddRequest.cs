namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Request
{
    public class LlmModel_AddRequest
    {
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Endpoint（必须）
        /// </summary>
        public string Endpoint { get; set; }

        /// <summary>
        /// 模型的类型（必须）, 例如：OpenAI,Azure OpenAI,HuggingFace
        /// </summary>
        public string ModelType { get;  set; }

        /// <summary>
        /// ApiKey
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// OrganizationId
        /// </summary>
        public string OrganizationId { get; set; }

        public string ApiVersion { get; set; }
    }
}
