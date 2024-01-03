using Senparc.AI;
using Senparc.Ncf.Core.Models;

namespace Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto
{
    public class AIModelDto : DtoBase
    {
        /// <summary>
        /// 代号
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        /// 名称（必须）
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Endpoint（必须）
        /// </summary>
        public string Endpoint { get; set; }

        /// <summary>
        /// 模型的类型（必须）, 例如：NeuCharAI, OpenAI, Azure OpenAI, HuggingFace
        /// </summary>
        public AiPlatform AiPlatform { get; set; }


        /// <summary>
        /// OrganizationId（可选）
        /// </summary>
        public string OrganizationId { get; set; }

        /// <summary>
        /// ApiKey（可选）
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// ApiVersion（可选）
        /// </summary>
        public string ApiVersion { get; set; }

        /// <summary>
        /// Note（可选）
        /// </summary>
        public string Note { get; set; }

        /// <summary>
        /// MaxToken（可选）
        /// </summary>
        public int MaxToken { get; set; }

        /// <summary>
        /// 是否共享
        /// </summary>
        public bool IsShared { get; set; } = false;

        private AIModelDto()
        {
        }
    }
}