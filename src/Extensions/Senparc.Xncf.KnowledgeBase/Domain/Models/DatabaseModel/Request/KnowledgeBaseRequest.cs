using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.KnowledgeBase.Domain.Models.DatabaseModel.Request
{
    public class KnowledgeBaseRequest
    {
        /// <summary>
        ///Training model ID
        /// </summary>
        public int EmbeddingModelId { get; set; }
        /// <summary>
        ///Vector databaseId
        /// </summary>
        public int VectorDBId { get; set; }
        /// <summary>
        /// Dialog model ID
        /// </summary>
        public int ChatModelId { get; set; }
        /// <summary>
        ///name
        /// </summary>
        public string Name { get; set; }
    }
}
