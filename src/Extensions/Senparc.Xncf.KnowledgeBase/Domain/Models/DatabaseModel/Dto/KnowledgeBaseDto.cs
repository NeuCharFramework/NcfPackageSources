
using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
namespace Senparc.Xncf.KnowledgeBase.Models.DatabaseModel.Dto
{
    public class KnowledgeBaseDto : DtoBase
    {
        public KnowledgeBaseDto()
        {
        }

        public KnowledgeBaseDto(int id, int embeddingModelId, int vectorDBId, int chatModelId, string name, string content)
        {
            Id = id;
            EmbeddingModelId = embeddingModelId;
            VectorDBId = vectorDBId;
            ChatModelId = chatModelId;
            Name = name;
            Content = content;
        }

        public int Id { get; set; }
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
        [MaxLength(100)]
        public string Name { get; set; }
        /// <summary>
        /// content
        /// </summary>
        public string Content { get; set; }

    }

    public class KnowledgeBase_InsertDto : KnowledgeBaseDto
    {
        public List<int> NcfFileIds { get; set; } = new List<int>();
    }
}
