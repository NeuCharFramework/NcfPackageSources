/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptItemTreeList.cs
    File description: PromptItemTreeList related implementation

    Created by: Senparc - 20240713

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.20.16 Localized PromptRange administration and runtime surfaces

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
        /// <summary>Initializes an empty prompt-item tree list.</summary>
        public PromptItemTreeList() { }

        public PromptItemTreeNode this[string key]
        {
            get
            {
                return this.FirstOrDefault(z => z.Key == key);
            }
        }

        /// <summary>Adds a tree node and returns the created node.</summary>
        /// <param name="key">The node key.</param>
        /// <param name="text">The display text.</param>
        /// <param name="value">The node value.</param>
        /// <param name="level">The tree level.</param>
        /// <param name="subNodeCount">The number of child nodes.</param>
        /// <returns>The added node.</returns>
        public PromptItemTreeNode AddNode(string key, string text, string value, int level, int subNodeCount)
        {
            PromptItemTreeNode node = new PromptItemTreeNode(key, text, value, level, subNodeCount);
            this.Add(node);
            return node;
        }
    }
}
