/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptItemTreeNode.cs
    File description: PromptItemTreeNode related implementation

    Created by: Senparc - 20240713

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.20.16 Localized PromptRange administration and runtime surfaces

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