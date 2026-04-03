using Senparc.AI;
using Senparc.Ncf.Core.Models;

namespace Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto
{
    public class LlModelDto : DtoBase
    {
        /// <summary>
        ///ID primary key
        /// </summary>
        public int Id { get; set; }

        public string Alias { get; set; }

        /// <summary>
        ///name (required)
        /// </summary>
        public string DeploymentName { get; set; }

        /// <summary>
        ///Type of model
        /// </summary>
        public AiPlatform ModelType { get; set; }

        /// <summary>
        ///Endpoint (required)
        /// </summary>
        public string Endpoint { get; set; }

        /// <summary>
        ///OrganizationId (optional)
        /// </summary>
        public string OrganizationId { get; set; }

        /// <summary>
        ///ApiKey (optional)
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        ///ApiVersion (optional)
        /// </summary>
        public string ApiVersion { get; set; }

        /// <summary>
        ///Note (optional)
        /// </summary>
        public string Note { get; set; }

        /// <summary>
        ///MaxToken (optional)
        /// </summary>
        public int MaxToken { get; set; }


        /// <summary>
        /// Whether to share
        /// </summary>
        public bool IsShared { get; set; }

        /// <summary>
        /// Whether to display
        /// </summary>
        public bool Show { get; set; }

        // /// <summary>
        // /// TextCompletionModelName (optional)
        // /// </summary>
        // public string TextCompletionModelName { get;  set; }
        //
        // /// <summary>
        // /// TextEmbeddingModelName (optional)
        // /// </summary>
        // public string TextEmbeddingModelName { get;  set; }
        //
        // /// <summary>
        // /// OtherModelName (optional)
        // /// </summary>
        // public string OtherModelName { get;  set; }

        private LlModelDto()
        {
        }
    }
}