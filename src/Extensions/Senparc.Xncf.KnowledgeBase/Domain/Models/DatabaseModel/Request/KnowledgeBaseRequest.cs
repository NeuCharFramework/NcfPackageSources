/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：KnowledgeBaseRequest.cs
    文件功能描述：KnowledgeBaseRequest 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

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
        public int Id { get; set; }
        /// <summary>
        /// 训练模型Id
        /// </summary>
        public int EmbeddingModelId { get; set; }
        /// <summary>
        /// 向量数据库Id
        /// </summary>
        public int VectorDBId { get; set; }
        /// <summary>
        /// 对话模型Id
        /// </summary>
        public int ChatModelId { get; set; }
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }

        public string Content { get; set; }

        /// <summary>
        /// null 表示不修改文件关联，空集合表示清空。
        /// </summary>
        public List<int> NcfFileIds { get; set; }
    }
}
