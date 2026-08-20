/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：KnowledgeBaseItemRequest.cs
    文件功能描述：KnowledgeBaseItemRequest 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Senparc.Xncf.KnowledgeBase.Models.DatabaseModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.KnowledgeBase.Domain.Models.DatabaseModel.Request
{
    public class KnowledgeBaseItemRequest
    {
        /// <summary>
        /// 知识库Id
        /// </summary>
        public int KnowledgeBasesId { get; set; }
        /// <summary>
        /// 内容类型
        /// </summary>
        public ContentType ContentType { get; set; }
        /// <summary>
        /// 内容
        /// </summary>
        public string Content { get; set; }
    }
}
