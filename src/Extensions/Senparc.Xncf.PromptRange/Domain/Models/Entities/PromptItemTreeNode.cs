/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：PromptItemTreeNode.cs
    文件功能描述：PromptItemTreeNode 相关实现
    
    
    创建标识：Senparc - 20240713
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

namespace Senparc.Xncf.PromptRange.Domain.Models.Entities
{
    public class PromptItemTreeNode
    {
        public string Key { get; set; }
        public string Text { get; set; }
        public string Value { get; set; }
        public int Level { get; set; }
        public int SubNodeCount { get; set; }

        public PromptItemTreeNode(string key, string text, string value, int level, int subNodeCount)
        {
            Key = key;
            Text = text;
            Value = value;
            Level = level;
            SubNodeCount = subNodeCount;
        }

    }
}