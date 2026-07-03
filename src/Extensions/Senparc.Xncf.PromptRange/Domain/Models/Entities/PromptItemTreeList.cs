/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：PromptItemTreeList.cs
    文件功能描述：PromptItemTreeList 相关实现
    
    
    创建标识：Senparc - 20240713
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.PromptRange.Domain.Models.Entities
{
    public class PromptItemTreeList : List<PromptItemTreeNode>
    {
        public PromptItemTreeList() { }

        public PromptItemTreeNode this[string key]
        {
            get
            {
                return this.FirstOrDefault(z => z.Key == key);
            }
        }

        public PromptItemTreeNode AddNode(string key, string text, string value, int level, int subNodeCount)
        {
            PromptItemTreeNode node = new PromptItemTreeNode(key, text, value, level, subNodeCount);
            this.Add(node);
            return node;
        }
    }
}
