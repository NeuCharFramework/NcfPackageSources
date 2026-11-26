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

        /// <summary>Initializes a prompt-item tree node.</summary>
        /// <param name="key">The node key.</param>
        /// <param name="text">The display text.</param>
        /// <param name="value">The node value.</param>
        /// <param name="level">The tree level.</param>
        /// <param name="subNodeCount">The number of child nodes.</param>
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
