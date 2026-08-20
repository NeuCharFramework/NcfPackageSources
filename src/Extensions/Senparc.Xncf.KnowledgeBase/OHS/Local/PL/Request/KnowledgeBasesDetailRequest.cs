/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：KnowledgeBasesDetailRequest.cs
    文件功能描述：KnowledgeBasesDetailRequest 相关实现
    
    
    创建标识：Senparc - 20250717
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.KnowledgeBase.OHS.Local.PL.Request
{
    public class KnowledgeBasesDetailRequest
    {
        /// <summary>
        /// 知识库Id
        /// </summary>
        public string KnowledgeBasesId { get; set; }
        /// <summary>
        /// 内容类型
        /// </summary>
        public int ContentType { get; set; }
        /// <summary>
        /// 内容
        /// </summary>
        public string Content { get; set; }
    }
}
