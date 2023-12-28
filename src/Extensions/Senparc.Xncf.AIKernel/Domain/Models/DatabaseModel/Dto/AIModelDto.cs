using Senparc.AI;
using Senparc.Ncf.Core.Models;

namespace Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto
{
    public class AIModelDto : DtoBase
    {
        /// <summary>
        /// 名称（必须）
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Endpoint（必须）
        /// </summary>
        public string Endpoint { get; private set; }

        /// <summary>
        /// 模型的类型（必须）, 例如：NeuCharAI, OpenAI, Azure OpenAI, HuggingFace
        /// </summary>
        public AiPlatform AiPlatform { get; internal set; }


        /// <summary>
        /// OrganizationId（可选）
        /// </summary>
        public string OrganizationId { get; private set; }

        /// <summary>
        /// ApiKey（可选）
        /// </summary>
        public string ApiKey { get; private set; }

        /// <summary>
        /// ApiVersion（可选）
        /// </summary>
        public string ApiVersion { get; private set; }

        /// <summary>
        /// Note（可选）
        /// </summary>
        public string Note { get; private set; }

        /// <summary>
        /// MaxToken（可选）
        /// </summary>
        public int MaxToken { get; private set; }

        // /// <summary>
        // /// TextCompletionModelName（可选）
        // /// </summary>
        // public string TextCompletionModelName { get; private set; }
        //
        // /// <summary>
        // /// TextEmbeddingModelName（可选）
        // /// </summary>
        // public string TextEmbeddingModelName { get; private set; }
        //
        // /// <summary>
        // /// OtherModelName（可选）
        // /// </summary>
        // public string OtherModelName { get; private set; }

        private AIModelDto() { }
    }
}
