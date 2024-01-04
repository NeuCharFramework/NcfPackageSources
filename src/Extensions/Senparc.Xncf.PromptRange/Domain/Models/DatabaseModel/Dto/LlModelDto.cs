using Senparc.AI;
using Senparc.Ncf.Core.Models;

namespace Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto
{
    public class LlModelDto : DtoBase
    {
        /// <summary>
        /// ID 主键
        /// </summary>
        public int Id { get; set; }

        public string Alias { get;  set; }

        /// <summary>
        /// 名称（必须）
        /// </summary>
        public string DeploymentName { get; set; }

        /// <summary>
        /// 模型的类型
        /// </summary>
        public AiPlatform ModelType { get;  set; }

        /// <summary>
        /// Endpoint（必须）
        /// </summary>
        public string Endpoint { get; set; }

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
        public bool IsShared { get;  set; }
        
        /// <summary>
        /// 是否展示
        /// </summary>
        public bool Show { get;  set; }

        // /// <summary>
        // /// TextCompletionModelName（可选）
        // /// </summary>
        // public string TextCompletionModelName { get;  set; }
        //
        // /// <summary>
        // /// TextEmbeddingModelName（可选）
        // /// </summary>
        // public string TextEmbeddingModelName { get;  set; }
        //
        // /// <summary>
        // /// OtherModelName（可选）
        // /// </summary>
        // public string OtherModelName { get;  set; }

        private LlModelDto()
        {
        }
    }
}